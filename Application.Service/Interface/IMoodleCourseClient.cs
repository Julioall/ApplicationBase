using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Application.Domain.Model.Education.Dtos;

namespace Application.Service.Interface
{
    public interface IMoodleCourseClient
    {
        Task<IReadOnlyCollection<MoodleCourseDto>> GetUserCoursesAsync(int userId, string token, CancellationToken cancellationToken = default);
        Task<MoodleCategoryDto?> GetCategoryAsync(int categoryId, string token, CancellationToken cancellationToken = default);
        Task<IReadOnlyDictionary<int, MoodleCategoryDto>> GetCategoriesAsync(IEnumerable<int> categoryIds, string token, CancellationToken cancellationToken = default);
    }
}
