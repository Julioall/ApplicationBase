using Application.Domain.Model.ValueObjects;

namespace Application.Domain.Model.Students
{
    public class Student
    {
        public string? Id { get; set; }
        public required string FirstName { get; set; }
        public required string LastName { get; set; }
        public string? Email { get; set; }
        public string? IdNumber { get; set; }
        public string? Phone { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public Address? Address { get; set; }
        public string? Institution { get; set; }
        public string? Lang { get; set; }
        public string? TimeZone { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        public string FullName => $"{FirstName} {LastName}".Trim();
    }
}
