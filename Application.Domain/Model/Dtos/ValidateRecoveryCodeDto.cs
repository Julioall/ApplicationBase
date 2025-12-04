namespace Application.Domain.Model.Dtos
{
    public class ValidateRecoveryCodeDto
    {
        public string Code { get; set; } = string.Empty;
        public string? Email { get; set; }
    }
}
