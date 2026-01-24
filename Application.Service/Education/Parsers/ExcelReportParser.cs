using Application.Domain.Model.Education.Dtos;
using ClosedXML.Excel;
using System.Reflection;
using Microsoft.Extensions.Logging;

namespace Application.Service.Education.Parsers
{
    public interface IExcelReportParser
    {
        Task<List<EducationReportRow>> ParseAsync(Stream fileStream);
    }

    public class ExcelReportParser : IExcelReportParser
    {
        private readonly ILogger<ExcelReportParser> _logger;

        public ExcelReportParser(ILogger<ExcelReportParser> logger)
        {
            _logger = logger;
        }

        public async Task<List<EducationReportRow>> ParseAsync(Stream fileStream)
        {
            _logger.LogInformation("Iniciando parse do arquivo XLSX");
            var rows = new List<EducationReportRow>();
            var propertyMap = GetPropertyMap();

            try
            {
                using (var workbook = new XLWorkbook(fileStream))
                {
                    var worksheet = workbook.Worksheets.FirstOrDefault();
                    if (worksheet == null)
                    {
                        _logger.LogWarning("Nenhuma planilha encontrada no arquivo XLSX");
                        return rows;
                    }

                    _logger.LogDebug("Planilha encontrada com {RowCount} linhas", worksheet.RowsUsed().Count());

                    // Ler a primeira linha como cabeçalho
                    var headerRow = worksheet.Row(1);
                    var columnMappings = new Dictionary<int, PropertyInfo>();

                    // Mapear colunas do header para propriedades
                    for (int colIndex = 1; colIndex <= headerRow.CellsUsed().Count(); colIndex++)
                    {
                        var headerCell = headerRow.Cell(colIndex);
                        var cellValue = headerCell.Value;
                        var headerValue = cellValue.IsBlank ? string.Empty : cellValue.ToString()?.Trim() ?? string.Empty;
                        
                        if (!string.IsNullOrWhiteSpace(headerValue) && propertyMap.TryGetValue(headerValue, out var property))
                        {
                            columnMappings[colIndex] = property;
                            _logger.LogDebug("Coluna mapeada: {HeaderValue} -> {PropertyName}", headerValue, property.Name);
                        }
                    }

                    _logger.LogInformation("Total de colunas mapeadas: {ColumnCount}", columnMappings.Count);

                    // Processar linhas de dados (começando da linha 2)
                    for (int rowIndex = 2; rowIndex <= worksheet.RowsUsed().Count(); rowIndex++)
                    {
                        var excelRow = worksheet.Row(rowIndex);
                        var reportRow = new EducationReportRow();
                        var rowProcessed = false;

                        // Mapear valores das colunas para propriedades
                        foreach (var mapping in columnMappings)
                        {
                            try
                            {
                                var cell = excelRow.Cell(mapping.Key);
                                var cellValue = cell.Value;

                                if (!cellValue.IsBlank)
                                {
                                    // Tentar converter o valor para o tipo apropriado
                                    var convertedValue = ConvertValue(cellValue, mapping.Value.PropertyType);
                                    mapping.Value.SetValue(reportRow, convertedValue);
                                    rowProcessed = true;
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.LogDebug(ex, "Erro ao converter valor na linha {RowIndex}, coluna {ColumnIndex}", 
                                    rowIndex, mapping.Key);
                            }
                        }

                        if (rowProcessed)
                        {
                            rows.Add(reportRow);
                            _logger.LogDebug("Linha {RowIndex} processada: Aluno={Aluno}, CPF={Cpf}, UC={UnicidadeCurricular}", 
                                rowIndex, reportRow.Aluno, reportRow.Cpf, reportRow.UnidadeCurricular);
                        }
                    }

                    _logger.LogInformation("Parse concluído: {RowCount} linhas processadas", rows.Count);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro fatal ao fazer parse do arquivo XLSX");
                throw;
            }

            return await Task.FromResult(rows);
        }

        private static object? ConvertValue(object value, Type targetType)
        {
            if (value == null)
                return null;

            if (targetType == typeof(string))
                return value.ToString();

            if (targetType == typeof(decimal) || targetType == typeof(decimal?))
            {
                if (decimal.TryParse(value.ToString(), out var decimalValue))
                    return decimalValue;
                return null;
            }

            if (targetType == typeof(DateTime) || targetType == typeof(DateTime?))
            {
                // Tentar diferentes formatos de data
                if (value is double doubleValue)
                {
                    try
                    {
                        // Formato Excel OADate
                        return DateTime.FromOADate(doubleValue);
                    }
                    catch
                    {
                        return null;
                    }
                }

                var dateString = value.ToString();
                var formats = new[] 
                { 
                    "dd/MM/yyyy HH:mm:ss",
                    "dd/MM/yyyy",
                    "yyyy-MM-dd HH:mm:ss",
                    "yyyy-MM-dd",
                    "d/M/yyyy",
                    "d/M/yyyy H:mm"
                };

                if (DateTime.TryParseExact(dateString, formats, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var dateValue))
                    return dateValue;

                return null;
            }

            return Convert.ChangeType(value, targetType);
        }

        private static Dictionary<string, PropertyInfo> GetPropertyMap()
        {
            var type = typeof(EducationReportRow);

            return new Dictionary<string, PropertyInfo>(StringComparer.OrdinalIgnoreCase)
            {
                { "aluno", type.GetProperty(nameof(EducationReportRow.Aluno))! },
                { "cpf", type.GetProperty(nameof(EducationReportRow.Cpf))! },
                { "e-mail", type.GetProperty(nameof(EducationReportRow.Email))! },
                { "telefone 1", type.GetProperty(nameof(EducationReportRow.Telefone1))! },
                { "telefone 2", type.GetProperty(nameof(EducationReportRow.Telefone2))! },
                { "unidade", type.GetProperty(nameof(EducationReportRow.Unidade))! },
                { "curso", type.GetProperty(nameof(EducationReportRow.Curso))! },
                { "unidade curricular", type.GetProperty(nameof(EducationReportRow.UnidadeCurricular))! },
                { "atividade", type.GetProperty(nameof(EducationReportRow.Atividade))! },
                { "nota final", type.GetProperty(nameof(EducationReportRow.NotaFinal))! },
                { "data do envio", type.GetProperty(nameof(EducationReportRow.DataDoEnvio))! },
                { "data de correção", type.GetProperty(nameof(EducationReportRow.DataDeCorrecao))! },
                { "status da submissão", type.GetProperty(nameof(EducationReportRow.StatusDaSubmissao))! },
                { "restrição de envio", type.GetProperty(nameof(EducationReportRow.RestricaoDeEnvio))! },
                { "data de início", type.GetProperty(nameof(EducationReportRow.DataDeInicio))! },
                { "data de término", type.GetProperty(nameof(EducationReportRow.DataDeTermino))! },
                { "último acesso", type.GetProperty(nameof(EducationReportRow.UltimoAcesso))! },
                { "observações", type.GetProperty(nameof(EducationReportRow.Observacoes))! }
            };
        }
    }
}
