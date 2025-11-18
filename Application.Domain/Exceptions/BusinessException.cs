using System.Net;

namespace Application.Domain.Exceptions
{
    public class BusinessException : DomainException
    {
        public BusinessException(string message)
            : base(message, HttpStatusCode.BadRequest)
        {
        }
    }
}
