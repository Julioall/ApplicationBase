using Application.Domain.CQRS;
using Application.Domain.Exceptions;
using Application.Domain.Interface;
using Application.Domain.Localization;
using Application.Domain.Model;
using Application.Domain.Model.Dtos.User;
using Application.Domain.Model.User;
using Application.Service.CQRS.Commands;
using Application.Service.Service.Security;
using FluentValidation;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;

namespace Application.Service.CQRS.Handlers
{
    /// <summary>
    /// Handler para o CreateUserCommand.
    /// Responsável por criar um novo usuário no sistema.
    /// </summary>
    public class CreateUserCommandHandler : ICommandHandler<CreateUserCommand, CreateUserResponse>
    {
        private readonly IUserRepository _userRepository;
        private readonly IValidator<User> _userValidator;
        private readonly IValidator<PasswordInput> _passwordValidator;
        private readonly IStringLocalizer<SharedResource> _localizer;
        private readonly ILogger<CreateUserCommandHandler> _logger;

        public CreateUserCommandHandler(
            IUserRepository userRepository,
            IValidator<User> userValidator,
            IValidator<PasswordInput> passwordValidator,
            IStringLocalizer<SharedResource> localizer,
            ILogger<CreateUserCommandHandler> logger)
        {
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            _userValidator = userValidator ?? throw new ArgumentNullException(nameof(userValidator));
            _passwordValidator = passwordValidator ?? throw new ArgumentNullException(nameof(passwordValidator));
            _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<CreateUserResponse> Handle(CreateUserCommand command, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Criando novo usuário com email {Email}", command.Email);

            try
            {
                // Validar entrada
                _passwordValidator.ValidateAndThrow(new PasswordInput { Password = command.Password });

                // Verificar se email já existe
                var existingUser = await _userRepository.GetByEmailAsync(command.Email);
                if (existingUser != null)
                {
                    _logger.LogWarning("Tentativa de criar usuário com email já existente: {Email}", command.Email);
                    throw new ConflictException(_localizer["EmailAlreadyExists"]);
                }

                // Criar novo usuário
                var user = new User
                {
                    Account = new UserAccount
                    {
                        Email = command.Email,
                        Username = command.Username,
                        PasswordHash = SecureHash.HashSecret(command.Password),
                        DateJoined = DateTime.UtcNow
                    },
                    Profile = new UserProfile
                    {
                        Name = command.FullName
                    }
                };

                // Verificar se é o primeiro usuário
                var isFirstUser = !await _userRepository.AnyAsync();
                if (isFirstUser)
                {
                    user.Account.Permissions = ApplicationPermissions.All.ToList();
                    _logger.LogInformation("Primeiro usuário sendo criado, atribuindo todas as permissões");
                }
                else
                {
                    user.Account.Permissions = ApplicationPermissions.DefaultUserPermissions.ToList();
                }

                // Validar user
                _userValidator.ValidateAndThrow(user);

                // Persistir
                await _userRepository.AddAsync(user);

                _logger.LogInformation("Usuário criado com sucesso: {UserId}", user.Id);

                return new CreateUserResponse(
                    UserId: user.Id ?? string.Empty,
                    Email: user.Account.Email,
                    CreatedAt: user.Account.DateJoined ?? DateTime.UtcNow
                );
            }
            catch (ValidationException vex)
            {
                _logger.LogError(vex, "Validação falhou ao criar usuário");
                throw;
            }
            catch (ConflictException cex)
            {
                _logger.LogError(cex, "Conflito ao criar usuário");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro inesperado ao criar usuário com email {Email}", command.Email);
                throw;
            }
        }
    }
}
