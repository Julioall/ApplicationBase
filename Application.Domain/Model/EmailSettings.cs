namespace Application.Domain.Model
{
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
