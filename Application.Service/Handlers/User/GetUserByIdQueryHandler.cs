using Application.Domain.Model.User;
using Application.Service.Interface;
using MediatR;

namespace Application.Service.Handlers
{
    /// <summary>
    /// Query para obter usuário por ID.
    /// Implementa padrão Query do CQRS.
    /// </summary>
    public class GetUserByIdQueryHandler : IRequestHandler<GetUserByIdQuery, Domain.Model.User.User?>
    {
        private readonly IUserService _userService;

        public GetUserByIdQueryHandler(IUserService userService)
        {
            _userService = userService;
        }

        public async Task<Domain.Model.User.User?> Handle(GetUserByIdQuery request, CancellationToken cancellationToken)
        {
            return await _userService.GetByIdAsync(request.UserId);
        }
    }

    /// <summary>
    /// Query para obter usuário por ID.
    /// </summary>
    public class GetUserByIdQuery : IRequest<Domain.Model.User.User?>
    {
        public string UserId { get; set; }

        public GetUserByIdQuery(string userId)
        {
            UserId = userId;
        }
    }
}
