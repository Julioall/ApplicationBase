using Application.Domain.CQRS;

namespace Application.Service.CQRS.Commands
{
    /// <summary>
    /// Command para atualizar dados do usuário.
    /// </summary>
    public record UpdateUserCommand(
        string UserId,
        string Email,
        string FullName,
        string? PhoneNumber = null,
        string? Address = null
    ) : ICommand<UpdateUserResponse>;

    /// <summary>
    /// Response do UpdateUserCommand.
    /// </summary>
    public record UpdateUserResponse(
        string UserId,
        string Email,
        DateTime UpdatedAt
    );
}
