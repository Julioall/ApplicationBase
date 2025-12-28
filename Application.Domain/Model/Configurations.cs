namespace Application.Domain.Model
{
    public class Configurations
    {
        public string? Id { get; set; }
        public EmailSettings Email { get; set; } = new EmailSettings();
        public WhatsAppSettings WhatsApp { get; set; } = new WhatsAppSettings();
    }
}
