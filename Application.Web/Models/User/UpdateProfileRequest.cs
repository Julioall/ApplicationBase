using Microsoft.AspNetCore.Http;

namespace Application.Api.Models.User
{
    public class UpdateProfileRequest
    {
        public string? Name { get; set; }

        public IFormFile? ProfilePicture { get; set; }

        public bool RemoveProfilePicture { get; set; }

        public double? ProfilePictureOffsetX { get; set; }

        public double? ProfilePictureOffsetY { get; set; }

        public string? JobTitle { get; set; }

        public string? Department { get; set; }

        public string? Organization { get; set; }

        public string? Location { get; set; }

        public double? ProfilePictureScale { get; set; }
    }
}
