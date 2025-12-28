namespace Application.Domain.Model.Dtos
{
    public class CreateWhatsAppInstanceRequest
    {
        public string DisplayName { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
    }

    public class UpdateWhatsAppInstanceRequest
    {
        public string DisplayName { get; set; } = string.Empty;
    }

    public class WhatsAppInstanceResponse
    {
        public string? Id { get; set; }
        public string DisplayName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? OwnerUserId { get; set; }
        public string? PhoneNumber { get; set; }
    }

    public class WhatsAppInstanceStatusResponse
    {
        public string Status { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }

    public class WhatsAppInstanceQrResponse
    {
        public string QrCode { get; set; } = string.Empty;
    }
}
