using Application.Domain.Interface;
using Application.Domain.Interface.Education;
using Application.Domain.Interface.Students;
using Application.Domain.Model.Education;
using Application.Domain.Model.Education.Dtos;
using Application.Domain.Model.Students;
using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Application.Service.Education
{
    public interface IEducationReportImportProcessor
    {
        Task<EducationReportImportResult> ProcessRowsAsync(List<EducationReportRow> rows);
    }

    public class EducationReportImportProcessor : IEducationReportImportProcessor
    {
        private readonly IEducationRepository _educationRepository;
        private readonly IStudentRepository _studentRepository;
        private readonly IStudentUcPerformanceRepository _performanceRepository;
        private readonly ILogger<EducationReportImportProcessor> _logger;

        // Caches de sessão para reduzir requisições RavenDB
        private Dictionary<string, string> _schoolIdCache = new();  // schoolName -> Id
        private Dictionary<string, string> _programIdCache = new(); // schoolId:programName -> Id
        private Dictionary<string, string> _classIdCache = new();   // schoolId:programId:className -> Id
        private Dictionary<string, string> _ucIdCache = new();      // eadId -> Id
        private Dictionary<string, Student> _studentCache = new();  // cpf -> Student
        private Dictionary<string, StudentUcPerformance> _performanceCache = new(); // studentId:ucId -> Performance
        
        private const int PerformanceBatchSize = 50; // Carregue 50 desempenhos de uma vez
        private List<(string StudentId, string UcId)> _performanceBatchQueue = new(); // Fila para batch loading

        public EducationReportImportProcessor(
            IEducationRepository educationRepository,
            IStudentRepository studentRepository,
            IStudentUcPerformanceRepository performanceRepository,
            ILogger<EducationReportImportProcessor> logger)
        {
            _educationRepository = educationRepository;
            _studentRepository = studentRepository;
            _performanceRepository = performanceRepository;
            _logger = logger;
        }

        public async Task<EducationReportImportResult> ProcessRowsAsync(List<EducationReportRow> rows)
        {
            _logger.LogInformation("EducationReportImportProcessor: Iniciando processamento de {RowCount} linhas", rows.Count);
            
            // Limpar caches para novo import
            _schoolIdCache.Clear();
            _programIdCache.Clear();
            _classIdCache.Clear();
            _ucIdCache.Clear();
            _studentCache.Clear();
            _performanceCache.Clear();
            _performanceBatchQueue.Clear();
            
            var result = new EducationReportImportResult
            {
                RowsRead = rows.Count
            };

            foreach (var (row, index) in rows.Select((r, i) => (r, i + 2))) // +2 porque linha 1 é header
            {
                try
                {
                    _logger.LogDebug("EducationReportImportProcessor: Processando linha {LineNumber}: Aluno={Aluno}, UC={UC}", 
                        index, row.Aluno, row.UnidadeCurricular ?? "vazia");
                    
                    await ProcessRowAsync(row, result);
                    
                    // Flush performance cache a cada PerformanceBatchSize linhas
                    if (_performanceBatchQueue.Count >= PerformanceBatchSize)
                    {
                        _logger.LogDebug("EducationReportImportProcessor: Flushing {Count} desempenhos em batch", _performanceBatchQueue.Count);
                        await FlushPerformanceBatchAsync();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "EducationReportImportProcessor: Erro ao processar linha {LineNumber}: {Aluno}", 
                        index, row.Aluno);
                    
                    result.Errors.Add(new ImportError
                    {
                        RowNumber = index,
                        Message = ex.Message
                    });
                }
            }

            // Flush final para desempenhos pendentes
            if (_performanceBatchQueue.Count > 0)
            {
                _logger.LogDebug("EducationReportImportProcessor: Flushing final {Count} desempenhos em batch", _performanceBatchQueue.Count);
                await FlushPerformanceBatchAsync();
            }

            _logger.LogInformation("EducationReportImportProcessor: Processamento concluído. Total: {Total}, Criados: {Created}, Atualizados: {Updated}, Erros: {Errors}, Puladas: {Skipped}",
                result.RowsRead,
                result.StudentsCreated,
                result.StudentsUpdated,
                result.Errors.Count,
                result.SkippedRows.Count);

            return result;
        }

        private async Task FlushPerformanceBatchAsync()
        {
            if (!_performanceBatchQueue.Any())
                return;

            try
            {
                // Carregar todos os desempenhos da fila em uma única operação
                var batch = _performanceBatchQueue.ToList();
                _performanceBatchQueue.Clear();

                _logger.LogDebug("EducationReportImportProcessor: Carregando {Count} desempenhos em batch", batch.Count);
                
                var batchResults = await _performanceRepository.GetByStudentAndUcBatchAsync(batch);
                
                // Adicionar ao cache
                foreach (var kvp in batchResults)
                {
                    _performanceCache[kvp.Key] = kvp.Value;
                }

                _logger.LogDebug("EducationReportImportProcessor: {Count} desempenhos carregados em cache", batchResults.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "EducationReportImportProcessor: Erro ao fazer flush de desempenhos em batch");
                // Continue sem falhar
            }
        }

        private async Task ProcessRowAsync(EducationReportRow row, EducationReportImportResult result)
        {
            // Validar linha mínima
            if (string.IsNullOrWhiteSpace(row.Aluno) || string.IsNullOrWhiteSpace(row.Cpf))
            {
                _logger.LogWarning("EducationReportImportProcessor: Linha pulada - Aluno ou CPF vazio. Aluno={Aluno}", row.Aluno ?? "vazio");
                result.SkippedRows.Add($"Aluno ou CPF vazio");
                return;
            }

            _logger.LogDebug("EducationReportImportProcessor: Validação básica OK. Aluno={Aluno}, CPF={CPF}, UC={UC}",
                row.Aluno, row.Cpf, row.UnidadeCurricular ?? "vazia");

            // 1. Upsert School (Unidade) - com cache
            var schoolName = row.Unidade ?? "Sem Unidade";
            var schoolId = _schoolIdCache.ContainsKey(schoolName) 
                ? _schoolIdCache[schoolName] 
                : (await _educationRepository.UpsertSchoolAsync(row.Unidade ?? "Sem Unidade")).Id;
            
            _schoolIdCache[schoolName] = schoolId;
            _logger.LogDebug("EducationReportImportProcessor: Unidade (School) criada/atualizada: {SchoolId} ({SchoolName})", 
                schoolId, schoolName);

            // 2. Upsert Program - com cache
            var programName = string.IsNullOrWhiteSpace(row.Curso) ? "Programa Padrão" : row.Curso;
            var programCacheKey = $"{schoolId}:{programName}";
            var programId = _programIdCache.ContainsKey(programCacheKey)
                ? _programIdCache[programCacheKey]
                : (await _educationRepository.UpsertProgramAsync(schoolId, programName)).Id;
            
            if (!_programIdCache.ContainsKey(programCacheKey))
            {
                result.ProgramsCreated++;
            }
            _programIdCache[programCacheKey] = programId;
            _logger.LogDebug("EducationReportImportProcessor: Programa criado/atualizado: {ProgramId} ({ProgramName})", 
                programId, programName);

            // 3. Upsert Class - com cache
            var className = string.IsNullOrWhiteSpace(row.Curso) ? "Turma Padrão" : row.Curso;
            var classCacheKey = $"{schoolId}:{programId}:{className}";
            var classId = _classIdCache.ContainsKey(classCacheKey)
                ? _classIdCache[classCacheKey]
                : (await _educationRepository.UpsertClassAsync(schoolId, programId, className, className)).Id;
            
            if (!_classIdCache.ContainsKey(classCacheKey))
            {
                result.ClassesCreated++;
            }
            _classIdCache[classCacheKey] = classId;
            _logger.LogDebug("EducationReportImportProcessor: Turma criada/atualizada: {ClassId} ({ClassName})", 
                classId, className);

            // 4. Upsert UC - com cache
            if (!string.IsNullOrWhiteSpace(row.UnidadeCurricular))
            {
                _logger.LogDebug("EducationReportImportProcessor: Processando UC: {UC}", row.UnidadeCurricular);
                
                // Gerar EadId estável e determinístico
                var eadId = GetDeterministicPositiveHash(row.UnidadeCurricular);
                
                var ucCacheKey = eadId.ToString();
                string ucId;
                
                if (_ucIdCache.ContainsKey(ucCacheKey))
                {
                    // UC já foi processada nesta sessão, usar do cache
                    ucId = _ucIdCache[ucCacheKey];
                    _logger.LogDebug("EducationReportImportProcessor: UC encontrada em cache: {UcCacheKey}", ucCacheKey);
                }
                else
                {
                    // UC não está em cache, fazer upsert
                    var ucResult = await _educationRepository.UpsertUcAsync(new UcDocument
                    {
                        EadId = eadId,
                        Fullname = row.UnidadeCurricular,
                        StartDate = ConvertToTimestamp(row.DataDeInicio),
                        EndDate = ConvertToTimestamp(row.DataDeTermino),
                        CourseCategory = row.UnidadeCurricular
                    });
                    
                    ucId = ucResult.Entity!.Id;
                    _logger.LogDebug("EducationReportImportProcessor: UcUpsertResult - Created={Created}, Updated={Updated}, Id={UcId}", 
                        ucResult.Created, ucResult.Updated, ucId);
                    
                    if (ucResult.Created)
                    {
                        result.UcsCreated++;
                        _logger.LogDebug("EducationReportImportProcessor: UC criada: {UcId} ({UcName})", ucId, row.UnidadeCurricular);
                    }
                    else if (ucResult.Updated)
                    {
                        result.UcsUpdated++;
                        _logger.LogDebug("EducationReportImportProcessor: UC atualizada: {UcId} ({UcName})", ucId, row.UnidadeCurricular);
                    }
                    else
                    {
                        _logger.LogDebug("EducationReportImportProcessor: UC encontrada (não criada/atualizada): {UcId} ({UcName})", ucId, row.UnidadeCurricular);
                    }
                    
                    _ucIdCache[ucCacheKey] = ucId;
                }

                // 5. Ensure Class-UC Link
                await _educationRepository.EnsureClassUcMapAsync(classId, ucId);
                _logger.LogDebug("EducationReportImportProcessor: Vínculo Turma-UC criado/verificado: Turma={ClassId}, UC={UcId}", 
                    classId, ucId);

                // 6. Upsert Student
                var studentId = await UpsertStudentAsync(row, result);
                if (string.IsNullOrWhiteSpace(studentId))
                {
                    _logger.LogWarning("EducationReportImportProcessor: Falha ao criar/atualizar estudante: {Aluno} (CPF={CPF}, UC={UC})", 
                        row.Aluno, row.Cpf, row.UnidadeCurricular);
                    result.SkippedRows.Add($"Não foi possível criar/atualizar estudante: {row.Aluno}");
                    return;
                }

                _logger.LogDebug("EducationReportImportProcessor: Estudante processado: {StudentId} ({StudentName})", 
                    studentId, row.Aluno);

                // 7. Ensure Student-UC Link
                await _educationRepository.EnsureStudentUcMapAsync(studentId, ucId);
                _logger.LogDebug("EducationReportImportProcessor: Vínculo Estudante-UC criado/verificado: Student={StudentId}, UC={UcId}", 
                    studentId, ucId);

                // 8. Upsert Performance
                await UpsertPerformanceAsync(studentId, ucId, row, result);
                _logger.LogDebug("EducationReportImportProcessor: Desempenho do estudante processado: Student={StudentId}, UC={UcId}", 
                    studentId, ucId);
            }
            else
            {
                _logger.LogWarning("EducationReportImportProcessor: Linha pulada - UC vazia. Aluno={Aluno}, UC={UC}", 
                    row.Aluno, row.UnidadeCurricular ?? "vazia");
                result.SkippedRows.Add($"Unidade Curricular vazia para estudante: {row.Aluno}");
            }
        }

        private async Task<string?> UpsertStudentAsync(EducationReportRow row, EducationReportImportResult result)
        {
            try
            {
                var cpf = NormalizeCpf(row.Cpf);
                _logger.LogDebug("EducationReportImportProcessor: Buscando estudante por CPF: {CPF}", cpf);
                
                // Verificar cache primeiro
                var cacheKey = $"student_cpf:{cpf}";
                if (_studentCache.ContainsKey(cacheKey))
                {
                    var cached = _studentCache[cacheKey];
                    _logger.LogDebug("EducationReportImportProcessor: Estudante encontrado em cache: {StudentId} ({StudentName})", 
                        cached.Id, cached.FirstName);
                    return cached.Id;
                }

                // Buscar aluno pelo documento de forma indexada
                var existing = await _studentRepository.GetByIdNumberAsync(cpf);

                if (existing != null)
                {
                    _logger.LogDebug("EducationReportImportProcessor: Estudante existente encontrado: {StudentId} ({StudentName})", 
                        existing.Id, existing.FirstName);
                    
                    // Cachear
                    _studentCache[cacheKey] = existing;
                    
                    // Update existing student
                    var parts = (row.Aluno ?? "Aluno").Split(new[] { ' ' }, 2);
                    existing.FirstName = parts[0];
                    existing.LastName = parts.Length > 1 ? parts[1] : existing.LastName;
                    existing.Email = row.Email ?? existing.Email;
                    existing.Phone = row.Telefone1 ?? existing.Phone;
                    existing.Phone2 = row.Telefone2 ?? existing.Phone2;
                    existing.UpdatedAt = DateTime.UtcNow;
                    
                    await _studentRepository.UpdateAsync(existing);
                    _logger.LogDebug("EducationReportImportProcessor: Estudante atualizado: {StudentId}", existing.Id);
                    result.StudentsUpdated++;
                    return existing.Id;
                }

                // Create new student
                _logger.LogDebug("EducationReportImportProcessor: Criando novo estudante: {StudentName} ({CPF})", 
                    row.Aluno, cpf);
                
                var parts2 = (row.Aluno ?? "Aluno").Split(new[] { ' ' }, 2);
                var student = new Student
                {
                    Id = Guid.NewGuid().ToString(),
                    FirstName = parts2[0],
                    LastName = parts2.Length > 1 ? parts2[1] : "Importado",
                    Email = row.Email ?? string.Empty,
                    IdNumber = cpf,
                    Phone = row.Telefone1 ?? string.Empty,
                    Phone2 = row.Telefone2 ?? string.Empty,
                    IsActive = true,
                    Status = StudentStatus.Active,
                    CreatedAt = DateTime.UtcNow
                };

                await _studentRepository.CreateAsync(student);
                
                // Cachear novo estudante
                _studentCache[cacheKey] = student;
                
                _logger.LogDebug("EducationReportImportProcessor: Novo estudante criado: {StudentId} ({StudentName})", 
                    student.Id, student.FirstName);
                result.StudentsCreated++;
                return student.Id;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "EducationReportImportProcessor: Erro ao processar estudante: {StudentName} ({CPF})", 
                    row.Aluno, row.Cpf);
                return null;
            }
        }

        private async Task UpsertPerformanceAsync(string studentId, string ucId, EducationReportRow row, EducationReportImportResult result)
        {
            try
            {
                _logger.LogDebug("EducationReportImportProcessor: Buscando desempenho existente: Student={StudentId}, UC={UcId}", 
                    studentId, ucId);
                
                var cacheKey = $"{studentId}:{ucId}";
                
                // Tentar obter do cache primeiro
                StudentUcPerformance? existing = null;
                if (_performanceCache.ContainsKey(cacheKey))
                {
                    existing = _performanceCache[cacheKey];
                    _logger.LogDebug("EducationReportImportProcessor: Desempenho encontrado em cache");
                }
                else
                {
                    // Adicionar à fila de batch loading
                    _performanceBatchQueue.Add((studentId, ucId));
                    
                    // Se não está em cache ainda, fazer flush agora para carregar
                    if (_performanceBatchQueue.Count >= PerformanceBatchSize)
                    {
                        // Flush apenas se temos um lote cheio
                        await FlushPerformanceBatchAsync();
                        _performanceCache.TryGetValue(cacheKey, out existing);
                    }
                }

                var grade = ParseGrade(row.NotaFinal);
                var lastAccess = ParseExcelDate(row.UltimoAcesso);

                if (existing != null)
                {
                    _logger.LogDebug("EducationReportImportProcessor: Atualizando desempenho existente (in-place): Student={StudentId}, UC={UcId}, Grade={Grade}", 
                        studentId, ucId, grade);
                    
                    // Update existing IN-PLACE - Deixar RavenDB rastrear as mudanças
                    // Não chamar SaveAsync() porque o objeto está tracked na sessão
                    existing.LastAccessAt = lastAccess;
                    existing.FinalGrade = grade;
                    existing.Activities ??= new List<StudentActivity>();
                    
                    // Add or update activity
                    var activity = new StudentActivity
                    {
                        Name = row.Atividade ?? "Atividade",
                        FinalGrade = grade,
                        SubmittedAt = ParseExcelDate(row.DataDoEnvio),
                        CorrectedAt = ParseExcelDate(row.DataDeCorrecao),
                        SubmissionStatus = row.StatusDaSubmissao ?? string.Empty,
                        Restriction = row.RestricaoDeEnvio ?? string.Empty,
                        StartAt = ParseExcelDate(row.DataDeInicio),
                        EndAt = ParseExcelDate(row.DataDeTermino)
                    };

                    existing.Activities.Add(activity);
                    _logger.LogDebug("EducationReportImportProcessor: Desempenho atualizado com sucesso (tracked by session)");
                    result.PerformanceRecordsUpserted++;
                }
                else
                {
                    _logger.LogDebug("EducationReportImportProcessor: Criando novo desempenho: Student={StudentId}, UC={UcId}, Grade={Grade}", 
                        studentId, ucId, grade);
                    
                    // Create new performance e guardar no cache para não ser carregado novamente
                    var performance = new StudentUcPerformance
                    {
                        // Deixar RavenDB gerenciar o ID automaticamente
                        StudentId = studentId,
                        UcId = ucId,
                        LastAccessAt = lastAccess,
                        FinalGrade = grade,
                        Activities = new List<StudentActivity>
                        {
                            new StudentActivity
                            {
                                Name = row.Atividade ?? "Atividade",
                                FinalGrade = grade,
                                SubmittedAt = ParseExcelDate(row.DataDoEnvio),
                                CorrectedAt = ParseExcelDate(row.DataDeCorrecao),
                                SubmissionStatus = row.StatusDaSubmissao ?? string.Empty,
                                Restriction = row.RestricaoDeEnvio ?? string.Empty,
                                StartAt = ParseExcelDate(row.DataDeInicio),
                                EndAt = ParseExcelDate(row.DataDeTermino)
                            }
                        }
                    };

                    // Armazenar (vai usar Store para novo documento)
                    await _performanceRepository.SaveAsync(performance);
                    
                    // Adicionar ao cache para evitar carregamento posterior
                    _performanceCache[cacheKey] = performance;
                    
                    _logger.LogDebug("EducationReportImportProcessor: Novo desempenho criado com sucesso");
                    result.PerformanceRecordsUpserted++;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "EducationReportImportProcessor: Erro ao processar desempenho: Student={StudentId}, UC={UcId}", 
                    studentId, ucId);
                // Log error but continue
            }
        }

        private static decimal? ParseGrade(string? gradeStr)
        {
            if (string.IsNullOrWhiteSpace(gradeStr))
                return null;

            if (decimal.TryParse(gradeStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var grade))
                return grade;

            return null;
        }

        private static int GetDeterministicPositiveHash(string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return 0;

            var bytes = Encoding.UTF8.GetBytes(text.Trim());
            using var sha = SHA256.Create();
            var hash = sha.ComputeHash(bytes);

            var value = BitConverter.ToInt32(hash, 0);
            if (value == int.MinValue)
                value = int.MaxValue;
            if (value == 0)
                value = 1;

            return value < 0 ? -value : value;
        }

        private static string NormalizeCpf(string cpf)
        {
            if (string.IsNullOrWhiteSpace(cpf))
                return string.Empty;

            return new string(cpf.Where(char.IsDigit).ToArray());
        }

        private static DateTime? ParseExcelDate(object? dateObj)
        {
            if (dateObj == null)
                return null;

            // Try Excel OADate format
            if (dateObj is double doubleValue)
            {
                try
                {
                    return DateTime.FromOADate(doubleValue);
                }
                catch
                {
                    return null;
                }
            }

            var dateStr = dateObj.ToString();
            if (string.IsNullOrWhiteSpace(dateStr))
                return null;

            var formats = new[]
            {
                "dd/MM/yyyy HH:mm:ss",
                "dd/MM/yyyy",
                "yyyy-MM-dd HH:mm:ss",
                "yyyy-MM-dd",
                "d/M/yyyy",
                "d/M/yyyy H:mm"
            };

            if (DateTime.TryParseExact(dateStr, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
                return date;

            return null;
        }

        private static long ConvertToTimestamp(object? dateObj)
        {
            var date = ParseExcelDate(dateObj);
            if (!date.HasValue)
                return DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            return ((DateTimeOffset)date.Value).ToUnixTimeSeconds();
        }
    }
}
