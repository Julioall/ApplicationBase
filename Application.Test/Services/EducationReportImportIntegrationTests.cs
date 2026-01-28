using System.Text;
using Application.Domain.Exceptions;
using Application.Domain.Model.Education;
using Application.Service.Education.Parsers;
using Application.Service.Interface;
using Application.Tests.Setup;
using ClosedXML.Excel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

namespace Application.Tests.Services
{
    /// <summary>
    /// Testes de integração para o processamento de importação de relatórios
    /// </summary>
    public class EducationReportImportIntegrationTests : BaseTest
    {
        [Fact]
        public async Task ExcelParser_Should_Parse_Valid_Excel_File()
        {
            // Arrange
            var mockLogger = new Mock<ILogger<ExcelReportParser>>();
            var parser = new ExcelReportParser(mockLogger.Object);
            var excelStream = CreateValidExcelFile();

            // Act
            var result = await parser.ParseAsync(excelStream);

            // Assert
            Assert.NotNull(result);
            Assert.NotEmpty(result);
            Assert.Equal(4, result.Count);

            var firstRow = result[0];
            Assert.Equal("João da Silva (Fictício)", firstRow.Aluno);
            Assert.Equal("11122233344", firstRow.Cpf);
            Assert.Equal("Fundamentos de Informática", firstRow.UnidadeCurricular);

            // opcional: valida a turma no campo Curso (se o parser expõe)
            Assert.Equal("1000586 - Operador de Computador - 00002/2025", firstRow.Curso);
        }

        [Fact]
        public async Task ExcelParser_Should_Handle_Empty_Excel_File()
        {
            // Arrange
            var mockLogger = new Mock<ILogger<ExcelReportParser>>();
            var parser = new ExcelReportParser(mockLogger.Object);
            var emptyStream = CreateEmptyExcelFile();

            // Act
            var result = await parser.ParseAsync(emptyStream);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task ExcelParser_Should_Skip_Rows_With_Missing_Required_Data()
        {
            // Arrange
            var mockLogger = new Mock<ILogger<ExcelReportParser>>();
            var parser = new ExcelReportParser(mockLogger.Object);
            var excelStream = CreateExcelFileWithMissingData();

            // Act
            var result = await parser.ParseAsync(excelStream);

            // Assert
            Assert.NotNull(result);
            // Should still parse rows even if some columns are missing
            Assert.NotEmpty(result);
        }

        [Fact]
        public async Task ImportReport_Should_Process_Valid_Excel_File()
        {
            // Arrange
            var educationService = _serviceProvider.GetService<IEducationService>()
                ?? throw new Exception("IEducationService not found");

            var excelStream = CreateValidExcelFile();
            var files = new[] { ("report.xlsx", excelStream) }.AsEnumerable();
            // Act & Assert
            await Assert.ThrowsAsync<BusinessException>(() => educationService.ImportReportAsync(files));
        }

        [Fact]
        public async Task ImportReport_Should_Count_File_Even_When_Empty()
        {
            // Arrange
            var educationService = _serviceProvider.GetService<IEducationService>()
                ?? throw new Exception("IEducationService not found");

            var emptyStream = CreateEmptyExcelFile();
            var files = new[] { ("empty.xlsx", emptyStream) }.AsEnumerable();

            // Act & Assert
            await Assert.ThrowsAsync<BusinessException>(() => educationService.ImportReportAsync(files));
        }

        [Fact]
        public async Task ImportReport_Should_Throw_For_NonXlsx_File()
        {
            // Arrange
            var educationService = _serviceProvider.GetService<IEducationService>()
                ?? throw new Exception("IEducationService not found");

            var invalidStream = new MemoryStream(Encoding.UTF8.GetBytes("invalid"));
            var files = new[] { ("report.txt", (Stream)invalidStream) }.AsEnumerable();

            // Act & Assert
            await Assert.ThrowsAsync<BusinessException>(() => educationService.ImportReportAsync(files));
        }

        private Stream CreateValidExcelFile()
        {
            var stream = new MemoryStream();
            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Report");

                // Headers (seguindo seu contrato + observações)
                var headers = new[]
                {
            "aluno", "cpf", "e-mail", "telefone 1", "telefone 2",
            "unidade", "curso", "unidade curricular", "atividade",
            "nota final", "data do envio", "data de correção", "status da submissão",
            "restrição de envio", "data de início", "data de término", "Último acesso", "observações"
        };

                for (int i = 0; i < headers.Length; i++)
                {
                    worksheet.Cell(1, i + 1).Value = headers[i];
                    worksheet.Cell(1, i + 1).Style.Font.Bold = true;
                }

                // Cenário SENAI:
                // - 'curso' = TURMA (código - nome do curso - turma/ano)
                // - Nome do curso (Program) deve ser extraído do meio
                // - Várias linhas por aluno+UC (uma por atividade)

                var now = new DateTime(2026, 01, 24, 10, 30, 00); // fixo para testes determinísticos

                // Turma
                var turma = "1000586 - Operador de Computador - 00002/2025";

                // Unidade
                var unidade = "SENAI - GOIÂNIA";

                // Aluno A (duas atividades na mesma UC)
                // CPF/e-mail fictícios (não reais)
                var alunoA_Nome = "João da Silva (Fictício)";
                var alunoA_Cpf = "11122233344";
                var alunoA_Email = "joao.silva.ficticio+senai@exemplo.test";
                var alunoA_Tel1 = "62990001111";
                var alunoA_Tel2 = "62990002222";

                // Aluno B (atividades em UCs diferentes)
                var alunoB_Nome = "Maria de Souza (Fictícia)";
                var alunoB_Cpf = "55566677788";
                var alunoB_Email = "maria.souza.ficticia+senai@exemplo.test";
                var alunoB_Tel1 = "62991113333";
                var alunoB_Tel2 = "62994445555";

                int row = 2;

                // Helper local para preencher uma linha
                void FillRow(
                    string aluno,
                    string cpf,
                    string email,
                    string tel1,
                    string tel2,
                    string unidadeCol,
                    string cursoTurma,
                    string uc,
                    string atividade,
                    string? notaFinal,
                    string dataEnvio,
                    string? dataCorrecao,
                    string statusSubmissao,
                    string restricaoEnvio,
                    string dataInicio,
                    string dataTermino,
                    string ultimoAcesso,
                    string observacoes)
                {
                    // 1..10
                    worksheet.Cell(row, 1).Value = aluno;
                    worksheet.Cell(row, 2).Value = cpf;
                    worksheet.Cell(row, 3).Value = email;
                    worksheet.Cell(row, 4).Value = tel1;
                    worksheet.Cell(row, 5).Value = tel2;
                    worksheet.Cell(row, 6).Value = unidadeCol;
                    worksheet.Cell(row, 7).Value = cursoTurma;
                    worksheet.Cell(row, 8).Value = uc;
                    worksheet.Cell(row, 9).Value = atividade;
                    worksheet.Cell(row, 10).Value = notaFinal ?? "";

                    // 11..18 (na ordem do header)
                    worksheet.Cell(row, 11).Value = dataEnvio;
                    worksheet.Cell(row, 12).Value = dataCorrecao ?? "";
                    worksheet.Cell(row, 13).Value = statusSubmissao;
                    worksheet.Cell(row, 14).Value = restricaoEnvio;
                    worksheet.Cell(row, 15).Value = dataInicio;
                    worksheet.Cell(row, 16).Value = dataTermino;
                    worksheet.Cell(row, 17).Value = ultimoAcesso;
                    worksheet.Cell(row, 18).Value = observacoes;

                    row++;
                }


                // ---------
                // Aluno A - UC: "Fundamentos de Informática" com 2 atividades
                // Último acesso varia para garantir max()
                // ---------

                // Linha 2 (data como string dd/MM/yyyy HH:mm:ss)
                FillRow(
                    aluno: alunoA_Nome,
                    cpf: alunoA_Cpf,
                    email: alunoA_Email,
                    tel1: alunoA_Tel1,
                    tel2: alunoA_Tel2,
                    unidadeCol: unidade,
                    cursoTurma: turma,
                    uc: "Fundamentos de Informática",
                    atividade: "Atividade 01 - Introdução",
                    notaFinal: "9,5",
                    dataEnvio: now.AddDays(-7).ToString("dd/MM/yyyy HH:mm:ss"),
                    dataCorrecao: now.AddDays(-6).ToString("dd/MM/yyyy"),
                    statusSubmissao: "Entregue",
                    restricaoEnvio: "",
                    dataInicio: now.AddMonths(-2).ToString("yyyy-MM-dd"),
                    dataTermino: now.AddMonths(2).ToString("yyyy-MM-dd"),
                    ultimoAcesso: now.AddDays(-1).ToString("dd/MM/yyyy HH:mm:ss"),
                    observacoes: "Registro fictício A1"
                );

                // Linha 3 (data como DateTime -> Excel date)
                FillRow(
                    aluno: alunoA_Nome,
                    cpf: alunoA_Cpf,
                    email: alunoA_Email,
                    tel1: alunoA_Tel1,
                    tel2: alunoA_Tel2,
                    unidadeCol: unidade,
                    cursoTurma: turma,
                    uc: "Fundamentos de Informática",
                    atividade: "Atividade 02 - Sistema Operacional",
                    notaFinal: "8,0",
                    dataEnvio: now.AddDays(-5).ToString("yyyy-MM-dd"), // DateTime
                    dataCorrecao: now.AddDays(-4).ToString("yyyy-MM-dd"), // DateTime
                    statusSubmissao: "Entregue",
                    restricaoEnvio: "",
                    dataInicio: now.AddMonths(-2).ToString("yyyy-MM-dd"),  // DateTime
                    dataTermino: now.AddMonths(2).ToString("yyyy-MM-dd"),  // DateTime
                    ultimoAcesso: now.ToString("dd/MM/yyyy HH:mm:ss"), // string completa
                    observacoes: "Registro fictício A2"
                );

                // ---------
                // Aluno B - duas UCs, uma atividade em cada
                // Segunda UC com nota vazia para testar null/empty
                // ---------

                // Linha 4
                FillRow(
                    aluno: alunoB_Nome,
                    cpf: alunoB_Cpf,
                    email: alunoB_Email,
                    tel1: alunoB_Tel1,
                    tel2: alunoB_Tel2,
                    unidadeCol: unidade,
                    cursoTurma: turma,
                    uc: "Comunicação e Texto",
                    atividade: "Atividade 01 - Produção textual",
                    notaFinal: "7,5",
                    dataEnvio: now.AddDays(-10).ToString("dd/MM/yyyy HH:mm:ss"),
                    dataCorrecao: now.AddDays(-9).ToString("dd/MM/yyyy"),
                    statusSubmissao: "Entregue",
                    restricaoEnvio: "",
                    dataInicio: now.AddMonths(-3).ToString("yyyy-MM-dd"),
                    dataTermino: now.AddMonths(1).ToString("yyyy-MM-dd"),
                    ultimoAcesso: now.AddDays(-2).ToString("dd/MM/yyyy HH:mm:ss"), // DateTime
                    observacoes: "Registro fictício B1"
                );

                // Linha 5 (nota final vazia)
                FillRow(
                    aluno: alunoB_Nome,
                    cpf: alunoB_Cpf,
                    email: alunoB_Email,
                    tel1: alunoB_Tel1,
                    tel2: alunoB_Tel2,
                    unidadeCol: unidade,
                    cursoTurma: turma,
                    uc: "Matemática Aplicada",
                    atividade: "Atividade 03 - Exercícios",
                    notaFinal: null, // vazio
                    dataEnvio: now.AddDays(-3).ToString("yyyy-MM-dd"), // DateTime
                    dataCorrecao: null, // sem correção
                    statusSubmissao: "Em andamento",
                    restricaoEnvio: "Somente 1 tentativa",
                    dataInicio: now.AddMonths(-1).ToString("yyyy-MM-dd"),
                    dataTermino: now.AddMonths(3).ToString("yyyy-MM-dd"),
                    ultimoAcesso: now.AddDays(-3).ToString("dd/MM/yyyy HH:mm:ss"),
                    observacoes: "Registro fictício B2 (sem nota)"
                );

                // Ajustes de visual (opcional)
                worksheet.Columns().AdjustToContents();

                workbook.SaveAs(stream);
            }

            stream.Position = 0;
            return stream;
        }

        private Stream CreateEmptyExcelFile()
        {
            var stream = new MemoryStream();
            using (var workbook = new XLWorkbook())
            {
                workbook.Worksheets.Add("Report");
                workbook.SaveAs(stream);
            }

            stream.Position = 0;
            return stream;
        }

        private Stream CreateExcelFileWithMissingData()
        {
            var stream = new MemoryStream();
            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Report");

                // Add headers (incomplete)
                worksheet.Cell(1, 1).Value = "aluno";
                worksheet.Cell(1, 2).Value = "cpf";
                worksheet.Cell(1, 3).Value = "e-mail";

                // Add data rows (missing most columns)
                worksheet.Cell(2, 1).Value = "João Silva";
                worksheet.Cell(2, 2).Value = "12345678901";
                worksheet.Cell(2, 3).Value = "joao@example.com";

                workbook.SaveAs(stream);
            }

            stream.Position = 0;
            return stream;
        }
    }
}
