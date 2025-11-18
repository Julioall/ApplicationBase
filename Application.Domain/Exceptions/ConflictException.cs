using System.Net;

namespace Application.Domain.Exceptions
{
    public class ConflictException : DomainException
    {
        public ConflictException(string message)
            : base(message, HttpStatusCode.Conflict)
        {
        }
    }
}
