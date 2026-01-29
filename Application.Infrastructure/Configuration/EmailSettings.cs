namespace Application.Infrastructure.Configuration
{
    /// <summary>
    /// Configurações para serviço de Email.
    /// Esta é uma classe ANÊMICA contendo apenas propriedades de configuração.
    /// Qualquer lógica de envio de email deve estar em Application.Infrastructure.ExternalServices.Email.EmailService.
    /// </summary>
    public class EmailSettings
    {
        public string FromName { get; set; } = "ApplicationBase";
        public string FromEmail { get; set; } = string.Empty;
        public string Host { get; set; } = string.Empty;
        public int Port { get; set; } = 587;
        public string Secure { get; set; } = "starttls";
        public string Password { get; set; } = string.Empty;
    }
}
