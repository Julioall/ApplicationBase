using System.Text.Json.Serialization;

namespace Application.Domain.Model.Education.Dtos
{
    /// <summary>
    /// DTO representing a Moodle course category from the API.
    /// </summary>
    public class MoodleCategoryDto
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("idnumber")]
        public string? IdNumber { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("parent")]
        public int Parent { get; set; }

        [JsonPropertyName("coursecount")]
        public int CourseCount { get; set; }

        [JsonPropertyName("visible")]
        public int Visible { get; set; }

        [JsonPropertyName("depth")]
        public int Depth { get; set; }

        [JsonPropertyName("path")]
        public string? Path { get; set; }
    }
}
