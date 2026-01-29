using System.Net.Http.Json;
using System.Text.Json;
using Application.Domain.Exceptions;
using Application.Domain.Localization;
using Application.Domain.Model;
using Application.Domain.Model.WhatsApp;
using Application.Service.Interface;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;

namespace Application.Service.Service
{
    public class EvolutionWhatsAppInstanceProvider : IWhatsAppInstanceProvider
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<EvolutionWhatsAppInstanceProvider> _logger;
        private readonly IStringLocalizer<SharedResource> _localizer;

        public EvolutionWhatsAppInstanceProvider(HttpClient httpClient, ILogger<EvolutionWhatsAppInstanceProvider> logger, IStringLocalizer<SharedResource> localizer)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
        }

        public async Task ProvisionAsync(WhatsAppInstance instance, CancellationToken cancellationToken = default)
        {
            var endpoint = BuildUrl("/instance/create");
            var payload = new
            {
                instanceName = instance.InternalInstanceName,
                displayName = instance.DisplayName,
                integration = ResolveIntegration(),
                number = instance.PhoneNumber
            };

            using var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
            {
                Content = JsonContent.Create(payload)
            };
            request.Headers.Add("apikey", ResolveApiKey());

            using var response = await _httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var details = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning("Evolution instance create failed. Status={StatusCode}, Response={Response}", (int)response.StatusCode, details);
                throw new BusinessException(_localizer["WhatsAppInstanceProvisionFailed"]);
            }
        }

        public async Task<string> GetQrCodeAsync(WhatsAppInstance instance, CancellationToken cancellationToken = default)
        {
            var endpoint = BuildUrl($"/instance/connect/{instance.InternalInstanceName}");
            var queryParts = new List<string>();
            if (!string.IsNullOrWhiteSpace(instance.PhoneNumber))
            {
                queryParts.Add($"number={Uri.EscapeDataString(instance.PhoneNumber)}");
            }
            queryParts.Add($"ts={DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}");
            endpoint = $"{endpoint}?{string.Join("&", queryParts)}";
            using var request = new HttpRequestMessage(HttpMethod.Get, endpoint);
            request.Headers.Add("apikey", ResolveApiKey());
            request.Headers.CacheControl = new System.Net.Http.Headers.CacheControlHeaderValue { NoCache = true };

            using var response = await _httpClient.SendAsync(request, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Evolution QR fetch failed. Status={StatusCode}, Response={Response}", (int)response.StatusCode, body);
                throw new BusinessException(_localizer["WhatsAppQrFetchFailed"]);
            }

            return ExtractString(body, new[] { "base64", "qrcode", "qr" }, nestedKey: "qrcode") ?? string.Empty;
        }

        public async Task<string> GetStatusAsync(WhatsAppInstance instance, CancellationToken cancellationToken = default)
        {
            var endpoint = BuildUrl($"/instance/connectionState/{instance.InternalInstanceName}");
            using var request = new HttpRequestMessage(HttpMethod.Get, endpoint);
            request.Headers.Add("apikey", ResolveApiKey());

            using var response = await _httpClient.SendAsync(request, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Evolution status failed. Status={StatusCode}, Response={Response}", (int)response.StatusCode, body);
                throw new BusinessException(_localizer["WhatsAppStatusFetchFailed"]);
            }

            var raw = ExtractString(
                body,
                new[] { "state", "status", "connectionState" },
                nestedKey: null,
                containerKeys: new[] { "instance", "data", "response" }) ?? string.Empty;
            return NormalizeStatus(raw);
        }

        public async Task DisconnectAsync(WhatsAppInstance instance, CancellationToken cancellationToken = default)
        {
            var endpoint = BuildUrl($"/instance/logout/{instance.InternalInstanceName}");
            using var request = new HttpRequestMessage(HttpMethod.Delete, endpoint);
            request.Headers.Add("apikey", ResolveApiKey());

            using var response = await _httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning("Evolution instance logout failed. Status={StatusCode}, Response={Response}", (int)response.StatusCode, body);
                throw new BusinessException(_localizer["WhatsAppInstanceDisconnectFailed"]);
            }
        }

        public async Task DeactivateAsync(WhatsAppInstance instance, CancellationToken cancellationToken = default)
        {
            var endpoint = BuildUrl($"/instance/delete/{instance.InternalInstanceName}");
            using var request = new HttpRequestMessage(HttpMethod.Delete, endpoint);
            request.Headers.Add("apikey", ResolveApiKey());

            using var response = await _httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning("Evolution instance delete failed. Status={StatusCode}, Response={Response}", (int)response.StatusCode, body);
            }
        }

        private static string NormalizeStatus(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
            {
                return WhatsAppInstanceStatus.Pending;
            }

            var normalized = raw.Trim().ToLowerInvariant();
            if (normalized.Contains("open") || normalized.Contains("connected"))
            {
                return WhatsAppInstanceStatus.Connected;
            }
            if (normalized.Contains("close") || normalized.Contains("disconnected") || normalized.Contains("offline"))
            {
                return WhatsAppInstanceStatus.Disconnected;
            }
            if (normalized.Contains("pending") || normalized.Contains("qr") || normalized.Contains("connecting"))
            {
                return WhatsAppInstanceStatus.Pending;
            }

            return WhatsAppInstanceStatus.Error;
        }

        private string ResolveApiKey()
        {
            var apiKey = Environment.GetEnvironmentVariable(ApplicationConstants.EVOLUTION_API_KEY);
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                throw new ConfigurationException(_localizer["WhatsAppApiKeyMissing"]);
            }

            return apiKey;
        }

        private string BuildUrl(string path)
        {
            var baseUrl = Environment.GetEnvironmentVariable(ApplicationConstants.EVOLUTION_API_BASE_URL);
            if (string.IsNullOrWhiteSpace(baseUrl))
            {
                throw new ConfigurationException(_localizer["WhatsAppBaseUrlMissing"]);
            }

            return $"{baseUrl.TrimEnd('/')}{path}";
        }

        private string ResolveIntegration()
        {
            var integration = Environment.GetEnvironmentVariable(ApplicationConstants.EVOLUTION_API_INTEGRATION);
            if (string.IsNullOrWhiteSpace(integration))
            {
                throw new ConfigurationException(_localizer["WhatsAppIntegrationMissing"]);
            }

            return integration;
        }

        private static string? ExtractString(string json, IEnumerable<string> keys, string? nestedKey = null, IEnumerable<string>? containerKeys = null)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return null;
            }

            try
            {
                using var document = JsonDocument.Parse(json);
                if (document.RootElement.ValueKind == JsonValueKind.Object)
                {
                    var direct = TryFindString(document.RootElement, keys);
                    if (!string.IsNullOrWhiteSpace(direct))
                    {
                        return direct;
                    }

                    if (!string.IsNullOrWhiteSpace(nestedKey) &&
                        document.RootElement.TryGetProperty(nestedKey, out var nested) &&
                        nested.ValueKind == JsonValueKind.Object)
                    {
                        var nestedValue = TryFindString(nested, keys);
                        if (!string.IsNullOrWhiteSpace(nestedValue))
                        {
                            return nestedValue;
                        }
                    }

                    if (containerKeys != null)
                    {
                        foreach (var container in containerKeys)
                        {
                            if (document.RootElement.TryGetProperty(container, out var containerElement))
                            {
                                var containerValue = TryFindString(containerElement, keys);
                                if (!string.IsNullOrWhiteSpace(containerValue))
                                {
                                    return containerValue;
                                }
                            }
                        }
                    }
                }
            }
            catch (JsonException)
            {
                return null;
            }

            return null;
        }

        private static string? TryFindString(JsonElement element, IEnumerable<string> keys)
        {
            if (element.ValueKind == JsonValueKind.String)
            {
                return element.GetString();
            }

            if (element.ValueKind == JsonValueKind.Object)
            {
                foreach (var key in keys)
                {
                    if (element.TryGetProperty(key, out var property))
                    {
                        if (property.ValueKind == JsonValueKind.String)
                        {
                            return property.GetString();
                        }

                        if (property.ValueKind == JsonValueKind.Object || property.ValueKind == JsonValueKind.Array)
                        {
                            var nested = TryFindString(property, keys);
                            if (!string.IsNullOrWhiteSpace(nested))
                            {
                                return nested;
                            }
                        }
                    }
                }
            }

            if (element.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in element.EnumerateArray())
                {
                    var nested = TryFindString(item, keys);
                    if (!string.IsNullOrWhiteSpace(nested))
                    {
                        return nested;
                    }
                }
            }

            return null;
        }
    }
}
