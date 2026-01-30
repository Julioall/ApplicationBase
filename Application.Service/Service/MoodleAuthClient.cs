using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Application.Domain.Model;
using Application.Service.Interface;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Application.Service.Service.Moodle
{
    public class MoodleAuthClient : IMoodleAuthClient
    {
        private readonly HttpClient _httpClient;
        private readonly MoodleSettings _settings;
        private readonly ILogger<MoodleAuthClient> _logger;

        public MoodleAuthClient(HttpClient httpClient, IOptions<MoodleSettings> settings, ILogger<MoodleAuthClient> logger)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<string?> AuthenticateAsync(string username, string password, CancellationToken cancellationToken = default)
        {
            if (!string.IsNullOrWhiteSpace(_settings.FixedToken))
            {
                return _settings.FixedToken;
            }

            if (string.IsNullOrWhiteSpace(_settings.BaseUrl) || string.IsNullOrWhiteSpace(_settings.ServiceName))
            {
                _logger.LogWarning("Moodle settings are missing BaseUrl or ServiceName");
                return null;
            }

            using var cts = CreateCancellation(cancellationToken);

            var url = $"{_settings.BaseUrl.TrimEnd('/')}/login/token.php?service={Uri.EscapeDataString(_settings.ServiceName)}&username={Uri.EscapeDataString(username)}&password={Uri.EscapeDataString(password)}";
            try
            {
                var response = await _httpClient.GetAsync(url, cts.Token);
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Moodle token endpoint returned status {Status}", response.StatusCode);
                    return null;
                }

                var payload = await response.Content.ReadFromJsonAsync<MoodleTokenResponse>(cancellationToken: cts.Token);
                return string.IsNullOrWhiteSpace(payload?.Token) ? null : payload.Token;
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Moodle authentication request timed out");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to authenticate on Moodle");
                return null;
            }
        }

        public async Task<MoodleSiteInfo?> GetSiteInfoAsync(string token, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(_settings.BaseUrl))
            {
                _logger.LogWarning("Moodle settings are missing BaseUrl");
                return null;
            }

            using var cts = CreateCancellation(cancellationToken);
            var url = $"{_settings.BaseUrl.TrimEnd('/')}/webservice/rest/server.php?wstoken={Uri.EscapeDataString(token)}&wsfunction=core_webservice_get_site_info&moodlewsrestformat=json";
            try
            {
                var response = await _httpClient.GetAsync(url, cts.Token);
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Moodle site info endpoint returned status {Status}", response.StatusCode);
                    return null;
                }

                var payload = await response.Content.ReadFromJsonAsync<MoodleSiteInfoResponse>(cancellationToken: cts.Token);
                if (payload == null || payload.UserId == 0)
                {
                    return null;
                }

                var enriched = await GetUserByFieldAsync(token, "id", payload.UserId.ToString(), cts.Token);
                if (enriched != null)
                {
                    payload.FullName ??= enriched.FullName;
                    payload.FirstName ??= enriched.FirstName;
                    payload.LastName ??= enriched.LastName;
                    payload.Email ??= enriched.Email;
                    payload.UserName ??= enriched.UserName;
                    payload.Department ??= enriched.Department;
                    payload.Institution ??= enriched.Institution;
                    payload.City ??= enriched.City;
                    payload.Country ??= enriched.Country;
                    payload.UserPictureUrl ??= enriched.ProfileImageUrl ?? enriched.ProfileImageUrlSmall;
                    payload.ProfileImageUrl ??= enriched.ProfileImageUrl;
                    payload.ProfileImageUrlSmall ??= enriched.ProfileImageUrlSmall;
                }

                var fullName = !string.IsNullOrWhiteSpace(payload.FullName)
                    ? payload.FullName
                    : BuildName(payload.FirstName, payload.LastName);

                var pictureUrl = payload.UserPictureUrl
                    ?? payload.ProfileImageUrl
                    ?? payload.ProfileImageUrlSmall;

                return new MoodleSiteInfo(
                    payload.UserId.ToString(),
                    fullName,
                    payload.Email ?? string.Empty,
                    payload.UserName ?? string.Empty,
                    pictureUrl,
                    payload.FirstName,
                    payload.LastName,
                    payload.City,
                    payload.Country,
                    payload.Department,
                    payload.Institution,
                    payload.Phone1,
                    payload.Phone2,
                    payload.Description,
                    payload.Lang,
                    payload.TimeZone,
                    payload.IdNumber);
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Moodle site info request timed out");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve Moodle site info");
                return null;
            }
        }

        private CancellationTokenSource CreateCancellation(CancellationToken cancellationToken)
        {
            var timeout = _settings.TimeoutSeconds > 0 ? TimeSpan.FromSeconds(_settings.TimeoutSeconds) : TimeSpan.FromSeconds(30);
            return CancellationTokenSource.CreateLinkedTokenSource(new CancellationTokenSource(timeout).Token, cancellationToken);
        }

        private static string BuildName(string? firstName, string? lastName)
        {
            var parts = new List<string>();
            if (!string.IsNullOrWhiteSpace(firstName))
            {
                parts.Add(firstName.Trim());
            }
            if (!string.IsNullOrWhiteSpace(lastName))
            {
                parts.Add(lastName.Trim());
            }

            return parts.Count > 0 ? string.Join(" ", parts) : string.Empty;
        }

        private sealed class MoodleTokenResponse
        {
            [JsonPropertyName("token")]
            public string? Token { get; set; }

            [JsonPropertyName("error")]
            public string? Error { get; set; }
        }

        private sealed class MoodleSiteInfoResponse
        {
            [JsonPropertyName("userid")]
            public int UserId { get; set; }

            [JsonPropertyName("fullname")]
            public string? FullName { get; set; }

            [JsonPropertyName("firstname")]
            public string? FirstName { get; set; }

            [JsonPropertyName("lastname")]
            public string? LastName { get; set; }

            [JsonPropertyName("email")]
            public string? Email { get; set; }

            [JsonPropertyName("username")]
            public string? UserName { get; set; }

            [JsonPropertyName("userpictureurl")]
            public string? UserPictureUrl { get; set; }

            [JsonPropertyName("profileimageurl")]
            public string? ProfileImageUrl { get; set; }

            [JsonPropertyName("profileimageurlsmall")]
            public string? ProfileImageUrlSmall { get; set; }

            [JsonPropertyName("city")]
            public string? City { get; set; }

            [JsonPropertyName("country")]
            public string? Country { get; set; }

            [JsonPropertyName("department")]
            public string? Department { get; set; }

            [JsonPropertyName("institution")]
            public string? Institution { get; set; }

            [JsonPropertyName("phone1")]
            public string? Phone1 { get; set; }

            [JsonPropertyName("phone2")]
            public string? Phone2 { get; set; }

            [JsonPropertyName("description")]
            public string? Description { get; set; }

            [JsonPropertyName("lang")]
            public string? Lang { get; set; }

            [JsonPropertyName("timezone")]
            public string? TimeZone { get; set; }

            [JsonPropertyName("idnumber")]
            public string? IdNumber { get; set; }
        }

        private sealed class MoodleUserFieldResponse
        {
            [JsonPropertyName("id")]
            public int Id { get; set; }

            [JsonPropertyName("fullname")]
            public string? FullName { get; set; }

            [JsonPropertyName("firstname")]
            public string? FirstName { get; set; }

            [JsonPropertyName("lastname")]
            public string? LastName { get; set; }

            [JsonPropertyName("email")]
            public string? Email { get; set; }

            [JsonPropertyName("username")]
            public string? UserName { get; set; }

            [JsonPropertyName("department")]
            public string? Department { get; set; }

            [JsonPropertyName("institution")]
            public string? Institution { get; set; }

            [JsonPropertyName("city")]
            public string? City { get; set; }

            [JsonPropertyName("country")]
            public string? Country { get; set; }

            [JsonPropertyName("profileimageurl")]
            public string? ProfileImageUrl { get; set; }

            [JsonPropertyName("profileimageurlsmall")]
            public string? ProfileImageUrlSmall { get; set; }
        }

        private async Task<MoodleUserFieldResponse?> GetUserByFieldAsync(string token, string field, string value, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(field) || string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            var baseUrl = _settings.BaseUrl?.TrimEnd('/');
            if (string.IsNullOrWhiteSpace(baseUrl))
            {
                return null;
            }

            var url = $"{baseUrl}/webservice/rest/server.php?wstoken={Uri.EscapeDataString(token)}&wsfunction=core_user_get_users_by_field&moodlewsrestformat=json&field={Uri.EscapeDataString(field)}&values[0]={Uri.EscapeDataString(value)}";

            try
            {
                var response = await _httpClient.GetAsync(url, cancellationToken);
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Moodle core_user_get_users_by_field returned status {Status}", response.StatusCode);
                    return null;
                }

                var users = await response.Content.ReadFromJsonAsync<List<MoodleUserFieldResponse>>(cancellationToken: cancellationToken);
                return users?.FirstOrDefault();
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Moodle core_user_get_users_by_field request timed out");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve Moodle user by field {Field}", field);
                return null;
            }
        }
    }

    public record MoodleSiteInfo(
        string UserId,
        string FullName,
        string Email,
        string UserName,
        string? PictureUrl,
        string? FirstName,
        string? LastName,
        string? City,
        string? Country,
        string? Department,
        string? Institution,
        string? Phone1,
        string? Phone2,
        string? Description,
        string? Lang,
        string? TimeZone,
        string? IdNumber);
}
