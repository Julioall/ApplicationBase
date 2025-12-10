using Application.Domain.Model.Dtos;
using Application.Domain.Model.Students;
using Application.Domain.Model.Students.Dtos;

namespace Application.Service.Interface
{
    public interface IStudentService
    {
        Task<Student> CreateStudentAsync(CreateStudentDto dto);
        Task<Student> UpdateStudentAsync(string id, UpdateStudentDto dto);
        Task DeleteStudentAsync(string id);
        Task<Student?> GetStudentAsync(string id);
        Task<PagedResult<Student>> GetStudentsAsync(PaginationQuery query);
    }
}
