using Application.Domain.Model.Dtos.Todo;

namespace Application.Domain.Model.Dtos.Todo
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




