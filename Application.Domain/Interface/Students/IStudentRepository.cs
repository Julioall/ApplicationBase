using Application.Domain.Model.Dtos;
using Application.Domain.Model.Students;

namespace Application.Domain.Interface.Students
{
    public interface IStudentRepository
    {
        Task<PagedResult<Student>> GetPagedAsync(PaginationQuery query);
        Task<IEnumerable<Student>> GetAllAsync();
        Task<Student?> GetByIdAsync(string id);
        Task<Student?> GetByIdNumberAsync(string idNumber);
        Task CreateAsync(Student student);
        Task UpdateAsync(Student student);
        Task DeleteAsync(string id);
        Task<bool> ExistsByIdNumberAsync(string idNumber, string? excludeId = null);
    }
}
