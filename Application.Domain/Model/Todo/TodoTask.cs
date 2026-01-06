namespace Application.Domain.Model.Todo
{
    public class TodoTask
    {
        public string Id { get; set; } = default!;
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public TodoStatus Status { get; set; } = TodoStatus.NotStarted;
        public string? Category { get; set; }
        public List<string> Categories { get; set; } = new();
        public int? Priority { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? DueDate { get; set; }
        public DateTime? CompletedAt { get; set; }
        public string? ContextType { get; set; }
        public string? ContextId { get; set; }
        public string? CreatedByUserId { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? AssignedToUserId { get; set; }
        public bool IsArchived { get; set; }
        public List<TodoStep> Steps { get; set; } = new();
    }
}
