using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Application.Domain.Model.Education.Dtos;

namespace Application.Service.Interface
{
    public interface IMoodleCourseClient
    {
        Task<IReadOnlyCollection<MoodleCourseDto>> GetUserCoursesAsync(int userId, string token, CancellationToken cancellationToken = default);
    }
}
