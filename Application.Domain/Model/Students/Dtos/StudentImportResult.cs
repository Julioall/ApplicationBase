namespace Application.Domain.Model.Students.Dtos
{
    public class StudentImportResult
    {
        public int Processed { get; set; }
        public int Created { get; set; }
        public int Updated { get; set; }
        public int Skipped { get; set; }
        public List<StudentImportError> Errors { get; set; } = new();
    }

    public class StudentImportError
    {
        public int Row { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
