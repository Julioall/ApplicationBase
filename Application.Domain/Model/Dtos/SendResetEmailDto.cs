namespace Application.Domain.Model.Dtos
{
    public class SendResetEmailDto
    {
        public string Email { get; set; } = string.Empty;
        public EmailSettings? Settings { get; set; }
    }
}
