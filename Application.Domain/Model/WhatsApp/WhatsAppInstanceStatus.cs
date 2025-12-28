namespace Application.Domain.Model.WhatsApp
{
    public static class WhatsAppInstanceStatus
    {
        public const string Pending = "pending";
        public const string Connected = "connected";
        public const string Disconnected = "disconnected";
        public const string Disabled = "disabled";
        public const string Error = "error";

        public static readonly IReadOnlyCollection<string> All = new[]
        {
            Pending,
            Connected,
            Disconnected,
            Disabled,
            Error
        };
    }
}
