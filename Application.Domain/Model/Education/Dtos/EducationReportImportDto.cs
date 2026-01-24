namespace Application.Domain.Model.Education.Dtos
{
    public class EducationReportRow
    {
        public string? Aluno { get; set; }
        public string? Cpf { get; set; }
        public string? Email { get; set; }
        public string? Telefone1 { get; set; }
        public string? Telefone2 { get; set; }
        public string? Unidade { get; set; }
        public string? Curso { get; set; }  // "1000586 - Operador de Computador - 00002/2025"
        public string? UnidadeCurricular { get; set; }
        public string? Atividade { get; set; }
        public string? NotaFinal { get; set; }
        public string? DataDoEnvio { get; set; }
        public string? DataDeCorrecao { get; set; }
        public string? StatusDaSubmissao { get; set; }
        public string? RestricaoDeEnvio { get; set; }
        public string? DataDeInicio { get; set; }
        public string? DataDeTermino { get; set; }
        public string? UltimoAcesso { get; set; }
        public string? Observacoes { get; set; }
    }

    public class EducationReportImportResult
    {
        public int FilesProcessed { get; set; }
        public int RowsRead { get; set; }
        public int ProgramsCreated { get; set; }
        public int ProgramsUpdated { get; set; }
        public int ClassesCreated { get; set; }
        public int ClassesUpdated { get; set; }
        public int UcsCreated { get; set; }
        public int UcsUpdated { get; set; }
        public int StudentsCreated { get; set; }
        public int StudentsUpdated { get; set; }
        public int UcLinksCreated { get; set; }
        public int UcLinksUpdated { get; set; }
        public int PerformanceRecordsUpserted { get; set; }
        public List<string> SkippedRows { get; set; } = new();
        public List<ImportError> Errors { get; set; } = new();
    }

    public class ImportError
    {
        public int? RowNumber { get; set; }
        public string? Message { get; set; }
    }
}
