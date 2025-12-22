namespace Application.Domain.Model.Education.Dtos
{
    public class CourseImportResult
    {
        public int Processed { get; set; }
        public int CreatedSchools { get; set; }
        public int CreatedPrograms { get; set; }
        public int CreatedClasses { get; set; }
        public int CreatedUcs { get; set; }
        public int UpdatedUcs { get; set; }
        public int Linked { get; set; }
        public List<CourseImportError> Errors { get; } = new();
    }

    public class CourseImportError
    {
        public string? CourseCategory { get; set; }
        public string? UcName { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
