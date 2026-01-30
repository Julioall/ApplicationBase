using Application.Service.Interface;
using MediatR;

namespace Application.Service.Handlers
{
    /// <summary>
    /// Handler para deletar usuário.
    /// Implementa padrão Command do CQRS.
    /// </summary>
    public class DeleteUserCommandHandler : IRequestHandler<DeleteUserCommand>
    {
        private readonly IUserService _userService;

        public DeleteUserCommandHandler(IUserService userService)
        {
            _userService = userService;
        }

        public async Task Handle(DeleteUserCommand request, CancellationToken cancellationToken)
        {
            await _userService.DeleteAsync(request.UserId);
        }
    }

    /// <summary>
    /// Command para deletar usuário.
    /// </summary>
    public class DeleteUserCommand : IRequest
    {
        public string UserId { get; set; }

        public DeleteUserCommand(string userId)
        {
            UserId = userId;
        }
    }
}
