using MediatR;

namespace Application.Domain.CQRS
{
    /// <summary>
    /// Base interface para Query Handlers.
    /// </summary>
    /// <typeparam name="TQuery">Tipo da query a ser processada.</typeparam>
    /// <typeparam name="TResponse">Tipo da resposta do handler.</typeparam>
    public interface IQueryHandler<TQuery, TResponse> : IRequestHandler<TQuery, TResponse>
        where TQuery : IQuery<TResponse>
    {
    }
}
