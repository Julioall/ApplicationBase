namespace Application.Domain.Model
{
    public class MoodleSettings
    {
        public string BaseUrl { get; set; } = string.Empty;
        public string ServiceName { get; set; } = string.Empty;
        public int TimeoutSeconds { get; set; } = 30;
        public string? FixedToken { get; set; }
    }
}
