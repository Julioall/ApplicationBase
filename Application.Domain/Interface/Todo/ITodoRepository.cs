using Application.Domain.Model.Todo;

namespace Application.Domain.Interface.Todo
{
    public interface ITodoRepository
    {
        Task<TodoTask?> GetByIdAsync(string id);
        Task<IReadOnlyList<TodoTask>> SearchAsync(TodoTaskSearchQuery query);
        Task AddAsync(TodoTask entity);
        Task UpdateAsync(TodoTask entity);
        Task DeleteAsync(string id);
    }
}
