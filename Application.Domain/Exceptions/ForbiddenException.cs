using System.Net;

namespace Application.Domain.Exceptions
{
    public class ForbiddenException : DomainException
    {
        public ForbiddenException(string message)
            : base(message, HttpStatusCode.Forbidden)
        {
        }
    }
}
