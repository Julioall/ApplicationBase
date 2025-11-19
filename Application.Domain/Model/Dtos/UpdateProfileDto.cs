namespace Application.Domain.Model.Dtos
{
    public class UpdateProfileDto
    {
        public string? Name { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string? ProfilePictureUrl { get; set; }
    }
}
