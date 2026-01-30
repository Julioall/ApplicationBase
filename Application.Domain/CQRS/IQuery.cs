using MediatR;

namespace Application.Domain.CQRS
{
    /// <summary>
    /// Base interface para Queries.
    /// </summary>
    /// <typeparam name="TResponse">Tipo da resposta da query.</typeparam>
    public interface IQuery<out TResponse> : IRequest<TResponse>
    {
    }
}
