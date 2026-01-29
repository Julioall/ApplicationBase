namespace Application.Infrastructure.Configuration
{
    /// <summary>
    /// Configurações para serviço de WhatsApp.
    /// Esta é uma classe ANÊMICA contendo apenas propriedades de configuração.
    /// Qualquer lógica de integração com WhatsApp deve estar em Application.Infrastructure.ExternalServices.WhatsApp.
    /// </summary>
    public class WhatsAppSettings
    {
        public string? ApiUrl { get; set; }
        public string? ApiKey { get; set; }
        public string? InstanceId { get; set; }
    }
}
