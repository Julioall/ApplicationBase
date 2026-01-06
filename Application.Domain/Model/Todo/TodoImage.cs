namespace Application.Domain.Model.Todo
{
    public class TodoImage
    {
        public string Id { get; set; } = default!;
        public string FileName { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public long Length { get; set; }
        public string? UploadedByUserId { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
