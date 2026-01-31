using Application.Domain.Localization;
using Application.Domain.Model;
using Application.Domain.Model.Dtos;
using Application.Domain.Model.User;
using Application.Service.Interface;
using MediatR;

namespace Application.Service.Handlers
{
    /// <summary>
    /// Handler para criar novo usuário.
    /// Implementa padrão Command do CQRS.
    /// </summary>
    public class CreateUserCommandHandler : IRequestHandler<CreateUserCommand, Domain.Model.User.User>
    {
        private readonly IUserService _userService;

        public CreateUserCommandHandler(IUserService userService)
        {
            _userService = userService;
        }

        public async Task<Domain.Model.User.User> Handle(CreateUserCommand request, CancellationToken cancellationToken)
        {
            var user = MapToUser(request.CreateUserDto);
            await _userService.AddAsync(user, request.CreateUserDto.Account.Password);
            return user;
        }

        private static Domain.Model.User.User MapToUser(CreateUserDto dto)
        {
            var permissions = NormalizePermissions(dto.Account.Permissions);
            if (!permissions.Any())
            {
                permissions = ApplicationPermissions.DefaultUserPermissions.ToList();
            }

            return new Domain.Model.User.User
            {
                Account = new UserAccount
                {
                    Email = dto.Account.Email,
                    Permissions = permissions,
                    DateJoined = dto.Account.DateJoined ?? DateTime.UtcNow
                },
                Profile = new UserProfile
                {
                    Name = dto.Profile.Name,
                    ProfilePictureUrl = dto.Profile.ProfilePictureUrl,
                    JobTitle = dto.Profile.JobTitle,
                    Department = dto.Profile.Department,
                    Organization = dto.Profile.Organization,
                    Location = dto.Profile.Location
                }
            };
        }

        private static List<string> NormalizePermissions(IEnumerable<string>? permissions)
        {
            if (permissions == null || !permissions.Any())
                return new List<string>();

            return permissions
                .Select(p => p?.Trim().ToUpperInvariant())
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .Distinct()
                .ToList() ?? new();
        }
    }

    /// <summary>
    /// Command para criar novo usuário.
    /// </summary>
    public class CreateUserCommand : IRequest<Domain.Model.User.User>
    {
        public CreateUserDto CreateUserDto { get; set; }

        public CreateUserCommand(CreateUserDto createUserDto)
        {
            CreateUserDto = createUserDto;
        }
    }
}
