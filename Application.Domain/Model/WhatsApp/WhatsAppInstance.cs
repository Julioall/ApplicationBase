namespace Application.Domain.Model.WhatsApp
{
    public class WhatsAppInstance
    {
        public string? Id { get; set; }
        public string DisplayName { get; set; } = string.Empty;
        public string InternalInstanceName { get; set; } = string.Empty;
        public string OwnerUserId { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public bool IsPrivateUserInstance { get; set; }
        public string Status { get; set; } = WhatsAppInstanceStatus.Pending;
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}
