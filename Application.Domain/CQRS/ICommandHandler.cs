using MediatR;

namespace Application.Domain.CQRS
{
    /// <summary>
    /// Base interface para Command Handlers sem retorno.
    /// </summary>
    /// <typeparam name="TCommand">Tipo do command a ser processado.</typeparam>
    public interface ICommandHandler<TCommand> : IRequestHandler<TCommand>
        where TCommand : ICommand
    {
    }

    /// <summary>
    /// Base interface para Command Handlers com retorno tipado.
    /// </summary>
    /// <typeparam name="TCommand">Tipo do command a ser processado.</typeparam>
    /// <typeparam name="TResponse">Tipo da resposta do handler.</typeparam>
    public interface ICommandHandler<TCommand, TResponse> : IRequestHandler<TCommand, TResponse>
        where TCommand : ICommand<TResponse>
    {
    }
}
