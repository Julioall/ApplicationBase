namespace Application.Domain.Model.Todo.Dtos
{
    public class UpdateTodoTaskDto
    {
        public string? Title { get; set; }
        public string? Description { get; set; }
        public TodoStatus? Status { get; set; }
        public string? Category { get; set; }
        public List<string>? Categories { get; set; }
        public int? Priority { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? DueDate { get; set; }
        public string? ContextType { get; set; }
        public string? ContextId { get; set; }
        public string? AssignedToUserId { get; set; }
    }
}
