namespace Application.Domain.Model.Education.Dtos
{
    public class MoodleCourseDto
    {
        public int Id { get; set; }
        public string FullName { get; set; }
        public string ShortName { get; set; }
        public string? DisplayName { get; set; }
        public string? IdNumber { get; set; }
        public string? Summary { get; set; }
        public int CategoryId { get; set; }
        public long? StartDate { get; set; }
        public long? EndDate { get; set; }
        public string? CourseImage { get; set; }
        public List<MoodleOverviewFileDto>? OverviewFiles { get; set; }

        // Novos campos trazidos do Moodle
        public float? Progress { get; set; }
        public bool? Completed { get; set; }
        public bool? IsFavourite { get; set; }
        public bool? Hidden { get; set; }
        public int? LastAccess { get; set; }
        public string? Lang { get; set; }

        public class MoodleOverviewFileDto
        {
            public string? FileUrl { get; set; }
            public string? FileName { get; set; }
            public string? FileMime { get; set; }
        }
    }
}
