using Application.Domain.Model.Dtos.Todo;

using Application.Domain.Model.Todo;

namespace Application.Domain.Model.Dtos.Todo
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
        public List<TodoAssigneeDto>? Assignees { get; set; }
        public bool? IsAllDay { get; set; }
        public TodoRecurrenceDto? Recurrence { get; set; }
        public bool ApplyToSeries { get; set; }
    }
}





