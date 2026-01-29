using System.Text.Json.Serialization;

using Application.Domain.Model.Dtos.Moodle;
namespace Application.Domain.Model.Dtos.Education
{
    public class MoodleCourseDto
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("fullname")]
        public string FullName { get; set; }

        [JsonPropertyName("shortname")]
        public string ShortName { get; set; }

        [JsonPropertyName("displayname")]
        public string? DisplayName { get; set; }

        [JsonPropertyName("idnumber")]
        public string? IdNumber { get; set; }

        [JsonPropertyName("summary")]
        public string? Summary { get; set; }

        [JsonPropertyName("category")]
        public int CategoryId { get; set; }

        [JsonPropertyName("categoryname")]
        public string? CategoryName { get; set; }

        [JsonPropertyName("startdate")]
        public long? StartDate { get; set; }

        [JsonPropertyName("enddate")]
        public long? EndDate { get; set; }

        [JsonPropertyName("courseimage")]
        public string? CourseImage { get; set; }

        [JsonPropertyName("overviewfiles")]
        public List<MoodleOverviewFileDto>? OverviewFiles { get; set; }

        // Novos campos trazidos do Moodle
        [JsonPropertyName("progress")]
        public float? Progress { get; set; }

        [JsonPropertyName("completed")]
        public bool? Completed { get; set; }

        [JsonPropertyName("isfavourite")]
        public bool? IsFavourite { get; set; }

        [JsonPropertyName("hidden")]
        public bool? Hidden { get; set; }

        [JsonPropertyName("lastaccess")]
        public int? LastAccess { get; set; }

        [JsonPropertyName("lang")]
        public string? Lang { get; set; }

        public class MoodleOverviewFileDto
        {
            [JsonPropertyName("fileurl")]
            public string? FileUrl { get; set; }

            [JsonPropertyName("filename")]
            public string? FileName { get; set; }

            [JsonPropertyName("mimetype")]
            public string? FileMime { get; set; }
        }
    }
}




