using MediatR;

namespace Application.Domain.CQRS
{
    /// <summary>
    /// Base interface para Commands sem retorno.
    /// </summary>
    public interface ICommand : IRequest
    {
    }

    /// <summary>
    /// Base interface para Commands com retorno tipado.
    /// </summary>
    /// <typeparam name="TResponse">Tipo da resposta do command.</typeparam>
    public interface ICommand<out TResponse> : IRequest<TResponse>
    {
    }
}
