using Application.Domain.Interface.Students;
using Application.Domain.Model.Dtos;
using Application.Domain.Model.Students;
using Application.Infrastructure.Indexes;
using Application.Infrastructure.Service;
using Raven.Client.Documents;
using Raven.Client.Documents.Linq;
using System.Linq;

namespace Application.Infrastructure.Repository.Students
{
    public class StudentRepository : IStudentRepository
    {
        private readonly IServiceRavenDB _serviceRavenDb;

        public StudentRepository(IServiceRavenDB serviceRavenDb)
        {
            _serviceRavenDb = serviceRavenDb;
        }

        public async Task CreateAsync(Student student)
        {
            await _serviceRavenDb.AsyncSession.StoreAsync(student);
        }

        public async Task DeleteAsync(string id)
        {
            var student = await _serviceRavenDb.AsyncSession.LoadAsync<Student>(id);
            if (student != null)
            {
                _serviceRavenDb.AsyncSession.Delete(student);
            }
        }

        public async Task<IEnumerable<Student>> GetAllAsync()
        {
            return await _serviceRavenDb.AsyncSession.Query<Student>().ToListAsync();
        }

        public async Task<Student?> GetByIdAsync(string id)
        {
            return await _serviceRavenDb.AsyncSession.LoadAsync<Student>(id);
        }

        public async Task<PagedResult<Student>> GetPagedAsync(PaginationQuery query)
        {
            var ravenQuery = _serviceRavenDb.AsyncSession.Query<Student>();

            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var term = query.Search.Trim();
                ravenQuery = ravenQuery
                    .Search(s => s.FirstName, term)
                    .Search(s => s.LastName, term)
                    .Search(s => s.Email, term)
                    .Search(s => s.IdNumber, term);
            }

            if (query.IsActive.HasValue)
            {
                var isActive = query.IsActive.Value;
                ravenQuery = ravenQuery.Where(s => s.IsActive == isActive);
            }

            var total = await ravenQuery.CountAsync();
            var students = await ravenQuery
                .OrderBy(s => s.LastName)
                .ThenBy(s => s.FirstName)
                .Skip((query.PageNumber - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToListAsync();

            return new PagedResult<Student>
            {
                Items = students,
                Total = total,
                PageNumber = query.PageNumber,
                PageSize = query.PageSize
            };
        }

        public async Task UpdateAsync(Student student)
        {
            await _serviceRavenDb.AsyncSession.StoreAsync(student);
        }

        public async Task<bool> ExistsByIdNumberAsync(string idNumber, string? excludeId = null)
        {
            var query = _serviceRavenDb.AsyncSession.Query<Student, Student_ByIdNumber>()
                .Customize(x => x.WaitForNonStaleResults())
                .Where(s => s.IdNumber == idNumber);

            if (!string.IsNullOrWhiteSpace(excludeId))
            {
                query = query.Where(s => s.Id != excludeId);
            }

            return await query.AnyAsync();
        }
    }
}
