namespace Application.Domain.Model.Dtos.Todo
{
    public class ReorderTodoStepsDto
    {
        public IReadOnlyCollection<string> StepIds { get; set; } = Array.Empty<string>();
    }
}



