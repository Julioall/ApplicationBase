using Application.Domain.CQRS;
using Application.Domain.Exceptions;
using Application.Domain.Interface;
using Application.Service.CQRS.Queries;
using Microsoft.Extensions.Logging;

namespace Application.Service.CQRS.Handlers
{
    /// <summary>
    /// Handler para GetUserByIdQuery.
    /// Busca usuário por ID no repositório.
    /// </summary>
    public class GetUserByIdQueryHandler : IQueryHandler<GetUserByIdQuery, GetUserByIdResponse>
    {
        private readonly IUserRepository _userRepository;
        private readonly ILogger<GetUserByIdQueryHandler> _logger;

        public GetUserByIdQueryHandler(
            IUserRepository userRepository,
            ILogger<GetUserByIdQueryHandler> logger)
        {
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<GetUserByIdResponse> Handle(GetUserByIdQuery query, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Buscando usuário com ID {UserId}", query.UserId);

            try
            {
                var user = await _userRepository.GetByIdAsync(query.UserId);
                
                if (user == null)
                {
                    _logger.LogWarning("Usuário não encontrado: {UserId}", query.UserId);
                    throw new NotFoundException($"Usuário com ID {query.UserId} não encontrado.");
                }

                _logger.LogDebug("Usuário encontrado: {UserId}", query.UserId);

                return new GetUserByIdResponse(
                    UserId: user.Id ?? string.Empty,
                    Email: user.Account.Email,
                    Username: user.Account.Username ?? string.Empty,
                    FullName: user.Profile.Name,
                    PhoneNumber: null,
                    Address: null,
                    CreatedAt: user.Account.DateJoined ?? DateTime.UtcNow,
                    UpdatedAt: null
                );
            }
            catch (NotFoundException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar usuário {UserId}", query.UserId);
                throw;
            }
        }
    }
}
