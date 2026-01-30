using Application.Domain.CQRS;
using Application.Domain.Interface;
using Application.Service.CQRS.Queries;
using Microsoft.Extensions.Logging;

namespace Application.Service.CQRS.Handlers
{
    /// <summary>
    /// Handler para GetUserByEmailQuery.
    /// Busca usuário por email no repositório.
    /// </summary>
    public class GetUserByEmailQueryHandler : IQueryHandler<GetUserByEmailQuery, GetUserByEmailResponse?>
    {
        private readonly IUserRepository _userRepository;
        private readonly ILogger<GetUserByEmailQueryHandler> _logger;

        public GetUserByEmailQueryHandler(
            IUserRepository userRepository,
            ILogger<GetUserByEmailQueryHandler> logger)
        {
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<GetUserByEmailResponse?> Handle(GetUserByEmailQuery query, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Buscando usuário com email {Email}", query.Email);

            try
            {
                var user = await _userRepository.GetByEmailAsync(query.Email);
                
                if (user == null)
                {
                    _logger.LogDebug("Usuário não encontrado para email: {Email}", query.Email);
                    return null;
                }

                _logger.LogDebug("Usuário encontrado para email: {Email}", query.Email);

                return new GetUserByEmailResponse(
                    UserId: user.Id ?? string.Empty,
                    Email: user.Account.Email,
                    Username: user.Account.Username ?? string.Empty,
                    FullName: user.Profile.Name,
                    CreatedAt: user.Account.DateJoined ?? DateTime.UtcNow
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar usuário por email {Email}", query.Email);
                throw;
            }
        }
    }
}
