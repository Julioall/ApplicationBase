using Application.Domain.CQRS;
using Application.Domain.Model.Dtos.User;

namespace Application.Service.CQRS.Commands
{
    /// <summary>
    /// Command para registrar/criar um novo usuário.
    /// </summary>
    public record CreateUserCommand(
        string Email,
        string Username,
        string Password,
        string FullName
    ) : ICommand<CreateUserResponse>;

    /// <summary>
    /// Response do CreateUserCommand.
    /// </summary>
    public record CreateUserResponse(
        string UserId,
        string Email,
        DateTime CreatedAt
    );
}
