using Application.Domain.Model.Todo;
using Application.Domain.Model.Todo.Dtos;
using System.IO;

namespace Application.Service.Interface
{
    public interface ITodoService
    {
        Task<IReadOnlyList<TodoTaskDto>> GetTasksAsync(TodoTaskSearchQuery query, string currentUserId);
        Task<TodoTaskDto> GetByIdAsync(string id);
        Task<TodoTaskDto> CreateAsync(string currentUserId, CreateTodoTaskDto dto);
        Task<TodoTaskDto> UpdateAsync(string id, UpdateTodoTaskDto dto);
        Task ArchiveAsync(string id);
        Task<TodoTaskDto> AddStepAsync(string taskId, CreateTodoStepDto dto);
        Task<TodoTaskDto> UpdateStepAsync(string taskId, string stepId, UpdateTodoStepDto dto);
        Task<TodoTaskDto> ReorderStepsAsync(string taskId, ReorderTodoStepsDto dto);
        Task<TodoTaskDto> DeleteStepAsync(string taskId, string stepId);
        Task<string> UploadImageAsync(string uploadedByUserId, Stream stream, string contentType, string fileName);
        Task<(byte[] Data, string ContentType, string FileName)?> GetImageAsync(string imageId);
        Task DeleteAsync(string id);
    }
}
