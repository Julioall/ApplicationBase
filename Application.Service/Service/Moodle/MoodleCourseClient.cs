using System.Net.Http;
using System.Net.Http.Json;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Application.Domain.Model.Education.Dtos;
using Application.Domain.Model;
using Application.Service.Interface;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Application.Service.Service.Moodle
{
    public class MoodleCourseClient : IMoodleCourseClient
    {
        private readonly HttpClient _httpClient;
        private readonly MoodleSettings _settings;
        private readonly ILogger<MoodleCourseClient> _logger;

        public MoodleCourseClient(HttpClient httpClient, IOptions<MoodleSettings> settings, ILogger<MoodleCourseClient> logger)
        {
            _httpClient = httpClient;
            _settings = settings.Value;
            _logger = logger;
        }

        public async Task<IReadOnlyCollection<MoodleCourseDto>> GetUserCoursesAsync(int userId, string token, CancellationToken cancellationToken = default)
        {
            var url = $"{_settings.BaseUrl.TrimEnd('/')}/webservice/rest/server.php?wstoken={token}&wsfunction=core_enrol_get_users_courses&moodlewsrestformat=json&userid={userId}";
            try
            {
                var response = await _httpClient.GetAsync(url, cancellationToken);
                response.EnsureSuccessStatusCode();
                var courses = await response.Content.ReadFromJsonAsync<List<MoodleCourseDto>>(cancellationToken: cancellationToken);
                return courses ?? new List<MoodleCourseDto>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar cursos do usuário no Moodle");
                throw;
            }
        }
    }
}
