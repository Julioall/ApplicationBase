using System;
using System.Text.Json.Serialization;

namespace Application.Domain.Model.User
{
    public class UserAccount
    {
        public required string Email { get; set; }
        [JsonIgnore]
        public string? PasswordHash { get; set; }
        public required string Role { get; set; }
        public DateTime? DateJoined { get; set; } = DateTime.UtcNow.Date;
        public DateTime? LastLogin { get; set; }
        [JsonIgnore]
        public string? RefreshTokenHash { get; set; }
        public string? RefreshTokenId { get; set; }
        public DateTime? RefreshTokenExpiry { get; set; }
    }
}
