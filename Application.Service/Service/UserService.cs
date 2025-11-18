using Application.Domain;
using Application.Domain.Exceptions;
using Application.Domain.Interface;
using Application.Domain.Model.User;
using Application.Service.Interface;
using FluentValidation;
using Microsoft.Extensions.Localization;

namespace Application.Service.Service
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;
        private readonly IValidator<User> _userValidator;
        private readonly IStringLocalizer<SharedResource> _localizer;

        public UserService(IUserRepository userRepository, IValidator<User> userValidator, IStringLocalizer<SharedResource> localizer)
        {
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            _userValidator = userValidator ?? throw new ArgumentNullException(nameof(userValidator));
            _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
        }

        public async Task AddAsync(User user)
        {
            ArgumentNullException.ThrowIfNull(user);

            _userValidator.ValidateAndThrow(user);

            var existingUser = await _userRepository.GetByEmailAsync(user.Account.Email);
            if (existingUser != null)
            {
                throw new ConflictException(_localizer["EmailAlreadyExists"]);
            }

            user.Account.DateJoined ??= DateTime.UtcNow;
            await _userRepository.AddAsync(user);
        }

        public Task DeleteAsync(string id)
        {
            return _userRepository.DeleteAsync(id);
        }

        public async Task UpdateAsync(User user)
        {
            ArgumentNullException.ThrowIfNull(user);

            _userValidator.ValidateAndThrow(user);
            await _userRepository.UpdateAsync(user);
        }

        public Task<IEnumerable<User>> GetAllAsync()
        {
            return _userRepository.GetAllAsync();
        }

        public Task<User> GetByIdAsync(string id)
        {
            return _userRepository.GetByIdAsync(id);
        }

        public Task<User> GetByEmailAsync(string email)
        {
            return _userRepository.GetByEmailAsync(email);
        }

        public Task<User> GetByRoleAsync(string role)
        {
            return _userRepository.GetByRoleAsync(role);
        }

        public Task<User> GetByRefreshTokenAsync(string refreshToken)
        {
            return _userRepository.GetByRefreshTokenAsync(refreshToken);
        }
    }
}
