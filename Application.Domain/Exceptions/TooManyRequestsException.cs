using System.Net;

namespace Application.Domain.Exceptions
{
    public class TooManyRequestsException : DomainException
    {
        public TooManyRequestsException(string message)
            : base(message, HttpStatusCode.TooManyRequests)
        {
        }
    }
}
