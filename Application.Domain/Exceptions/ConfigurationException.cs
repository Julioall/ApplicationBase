using System.Net;

namespace Application.Domain.Exceptions
{
    public class ConfigurationException : DomainException
    {
        public ConfigurationException(string message)
            : base(message, HttpStatusCode.InternalServerError)
        {
        }
    }
}
