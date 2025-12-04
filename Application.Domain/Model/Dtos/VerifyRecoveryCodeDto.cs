namespace Application.Domain.Model.Dtos
{
    public class VerifyRecoveryCodeDto
    {
        public string Code { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
        public string? Email { get; set; }
    }
}
