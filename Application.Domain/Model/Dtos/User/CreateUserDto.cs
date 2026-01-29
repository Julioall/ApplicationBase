using Application.Domain.Model.Dtos.User;

namespace Application.Domain.Model.Dtos.User
{
    public class CreateUserDto
    {
        public required CreateUserAccountDto Account { get; set; }
        public required CreateUserProfileDto Profile { get; set; }
    }

    public class CreateUserAccountDto
    {
        public required string Email { get; set; }
        public required string Password { get; set; }
        public List<string>? Permissions { get; set; }
        public DateTime? DateJoined { get; set; }
    }

    public class CreateUserProfileDto
    {
        public required string Name { get; set; }
        public string? ProfilePictureUrl { get; set; }
        public string? JobTitle { get; set; }
        public string? Department { get; set; }
        public string? Organization { get; set; }
        public string? Location { get; set; }
    }
}



