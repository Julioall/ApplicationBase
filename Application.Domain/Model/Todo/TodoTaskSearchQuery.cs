namespace Application.Domain.Model.Todo
{
    public class TodoTaskSearchQuery
    {
        public string? ContextType { get; set; }
        public string? ContextId { get; set; }
        public string? AssignedToUserId { get; set; }
        public bool IncludeArchived { get; set; }
    }
}
