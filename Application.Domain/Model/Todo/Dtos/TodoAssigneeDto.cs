namespace Application.Domain.Model.Todo.Dtos
{
    public class TodoAssigneeDto
    {
        public string? Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? AvatarUrl { get; set; }
    }
}
