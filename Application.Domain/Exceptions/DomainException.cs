using System;
using System.Net;

namespace Application.Domain.Exceptions
{
    public abstract class DomainException : Exception
    {
        public int StatusCode { get; }

        protected DomainException(string message, HttpStatusCode statusCode)
            : base(message)
        {
            StatusCode = (int)statusCode;
        }
    }
}
