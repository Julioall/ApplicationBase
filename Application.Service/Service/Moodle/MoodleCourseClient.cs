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

        public async Task<MoodleCategoryDto?> GetCategoryAsync(int categoryId, string token, CancellationToken cancellationToken = default)
        {
            var categories = await GetCategoriesAsync(new[] { categoryId }, token, cancellationToken);
            return categories.TryGetValue(categoryId, out var category) ? category : null;
        }

        public async Task<IReadOnlyDictionary<int, MoodleCategoryDto>> GetCategoriesAsync(IEnumerable<int> categoryIds, string token, CancellationToken cancellationToken = default)
        {
            var result = new Dictionary<int, MoodleCategoryDto>();
            var idsToFetch = categoryIds.Distinct().ToList();

            if (idsToFetch.Count == 0)
                return result;

            var idsValue = string.Join(",", idsToFetch);
            var url = $"{_settings.BaseUrl.TrimEnd('/')}/webservice/rest/server.php" +
                      $"?wstoken={token}" +
                      $"&wsfunction=core_course_get_categories" +
                      $"&moodlewsrestformat=json" +
                      $"&criteria[0][key]=ids" +
                      $"&criteria[0][value]={idsValue}";

            try
            {
                var response = await _httpClient.GetAsync(url, cancellationToken);
                response.EnsureSuccessStatusCode();
                var categories = await response.Content.ReadFromJsonAsync<List<MoodleCategoryDto>>(cancellationToken: cancellationToken);

                if (categories != null)
                {
                    foreach (var cat in categories)
                    {
                        result[cat.Id] = cat;
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Erro ao buscar categorias do Moodle. Retornando dicionário vazio.");
                return result;
            }
        }
    }
}
