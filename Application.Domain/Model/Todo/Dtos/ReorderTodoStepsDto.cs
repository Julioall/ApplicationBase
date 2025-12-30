namespace Application.Domain.Model.Todo.Dtos
{
    public class ReorderTodoStepsDto
    {
        public IReadOnlyCollection<string> StepIds { get; set; } = Array.Empty<string>();
    }
}
