using System.Net;

namespace Application.Domain.Exceptions
{
    public class NotFoundException : DomainException
    {
        public NotFoundException(string message)
            : base(message, HttpStatusCode.NotFound)
        {
        }
    }
}
