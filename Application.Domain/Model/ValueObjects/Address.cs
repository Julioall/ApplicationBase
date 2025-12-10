namespace Application.Domain.Model.ValueObjects
{
    public class Address
    {
        public string? Street { get; set; }
        public string? Number { get; set; }
        public string? District { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string? PostalCode { get; set; }
        public string? Country { get; set; }
    }
}
