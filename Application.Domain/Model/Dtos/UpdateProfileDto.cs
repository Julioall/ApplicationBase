namespace Application.Domain.Model.Dtos
{
    public class UpdateProfileDto
    {
        public string? Name { get; set; }

        public string? ProfilePictureUrl { get; set; }

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
