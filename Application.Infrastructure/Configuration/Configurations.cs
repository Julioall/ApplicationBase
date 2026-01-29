namespace Application.Infrastructure.Configuration
{
    /// <summary>
    /// Classe agregadora de configurações gerais da aplicação.
    /// Esta é uma classe ANÊMICA que apenas agrupa configurações.
    /// </summary>
    public class Configurations
    {
        public string? Id { get; set; }
        public EmailSettings Email { get; set; } = new EmailSettings();
        public WhatsAppSettings WhatsApp { get; set; } = new WhatsAppSettings();
    }
}
