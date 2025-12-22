namespace Application.Domain.Model.User
{
    public class User
    {
        public string? Id { get; set; }
        public UserAccount Account { get; set; } = new() { Email = string.Empty };
        public UserProfile Profile { get; set; } = new() { Name = string.Empty };
    }
}
