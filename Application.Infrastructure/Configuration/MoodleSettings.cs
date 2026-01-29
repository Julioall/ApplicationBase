namespace Application.Infrastructure.Configuration
{
    /// <summary>
    /// Configurações para integração com Moodle.
    /// Esta é uma classe ANÊMICA contendo apenas propriedades de configuração.
    /// Qualquer lógica de chamadas ao Moodle deve estar em Application.Infrastructure.ExternalServices.Moodle.
    /// </summary>
    public class MoodleSettings
    {
        public string BaseUrl { get; set; } = string.Empty;
        public string ServiceName { get; set; } = string.Empty;
        public int TimeoutSeconds { get; set; } = 30;
        public string? FixedToken { get; set; }
    }
}
