using Application.Domain.Model.Students;
using Application.Domain.Model.ValueObjects;

namespace Application.Domain.Model.Students.Dtos
{
    public class CreateStudentDto
    {
        public required string FirstName { get; set; }
        public required string LastName { get; set; }
        public string? Email { get; set; }
        public string? IdNumber { get; set; }
        public string? Phone { get; set; }
        public Address? Address { get; set; }
        public string? Institution { get; set; }
        public string? Lang { get; set; }
        public string? TimeZone { get; set; }
        public bool IsActive { get; set; } = true;
        public string Status { get; set; } = StudentStatus.Active;
        public DateTime? LastAccessAt { get; set; }
    }

    public class UpdateStudentDto
    {
        public required string FirstName { get; set; }
        public required string LastName { get; set; }
        public string? Email { get; set; }
        public string? IdNumber { get; set; }
        public string? Phone { get; set; }
        public Address? Address { get; set; }
        public string? Institution { get; set; }
        public string? Lang { get; set; }
        public string? TimeZone { get; set; }
        public bool IsActive { get; set; } = true;
        public string Status { get; set; } = StudentStatus.Active;
        public DateTime? LastAccessAt { get; set; }
    }
}
