using Application.Domain.CQRS;

namespace Application.Service.CQRS.Commands
{
    /// <summary>
    /// Command para alterar senha do usuário.
    /// </summary>
    public record ChangePasswordCommand(
        string UserId,
        string CurrentPassword,
        string NewPassword
    ) : ICommand<ChangePasswordResponse>;

    /// <summary>
    /// Response do ChangePasswordCommand.
    /// </summary>
    public record ChangePasswordResponse(
        bool Success,
        string Message,
        DateTime ChangedAt
    );
}
