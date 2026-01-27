namespace Application.Domain.Model.Dtos
{
    public record MoodleLoginDto
    {
        public required string Username { get; set; }
        public required string Password { get; set; }
    }
}
