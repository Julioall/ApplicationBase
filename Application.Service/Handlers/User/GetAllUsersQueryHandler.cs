using Application.Domain.Model.User;
using Application.Service.Interface;
using MediatR;

namespace Application.Service.Handlers
{
    /// <summary>
    /// Query para obter todos os usuários.
    /// Implementa padrão Query do CQRS.
    /// </summary>
    public class GetAllUsersQueryHandler : IRequestHandler<GetAllUsersQuery, IEnumerable<Domain.Model.User.User>>
    {
        private readonly IUserService _userService;

        public GetAllUsersQueryHandler(IUserService userService)
        {
            _userService = userService;
        }

        public async Task<IEnumerable<Domain.Model.User.User>> Handle(GetAllUsersQuery request, CancellationToken cancellationToken)
        {
            return await _userService.GetAllAsync();
        }
    }

    /// <summary>
    /// Query para obter todos os usuários.
    /// </summary>
    public class GetAllUsersQuery : IRequest<IEnumerable<Domain.Model.User.User>>
    {
    }
}
