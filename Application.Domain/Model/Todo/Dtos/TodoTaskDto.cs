namespace Application.Domain.Model.Todo.Dtos
{
    public class TodoTaskDto
    {
        public string Id { get; set; } = default!;
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public TodoStatus Status { get; set; }
        public string? Category { get; set; }
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
        public IReadOnlyCollection<TodoStepDto> Steps { get; set; } = Array.Empty<TodoStepDto>();
    }
}
