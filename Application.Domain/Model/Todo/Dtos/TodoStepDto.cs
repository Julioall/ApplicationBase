namespace Application.Domain.Model.Todo.Dtos
{
    public class TodoStepDto
    {
        public string Id { get; set; } = default!;
        public string Title { get; set; } = string.Empty;
        public bool IsCompleted { get; set; }
        public int Order { get; set; }
        public DateTime? CompletedAt { get; set; }
    }
}
