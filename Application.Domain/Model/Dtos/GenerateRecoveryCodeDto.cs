namespace Application.Domain.Model.Dtos
{
    public class GenerateRecoveryCodeDto
    {
        public bool SendEmail { get; set; } = true;
        public string? Email { get; set; }
    }
}
