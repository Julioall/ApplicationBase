using Application.Domain.Interface;
using Application.Domain.Model.User;
using FluentValidation;
using Microsoft.Extensions.Localization;

namespace Application.Domain.Validator
{
    public class UserValidator : AbstractValidator<User>
    {
        private readonly IServiceRavenDB _serviceRavenDB;
        private readonly IStringLocalizer<SharedResource> _localizer;

        public UserValidator(IServiceRavenDB serviceRavenDB, IStringLocalizer<SharedResource> localizer)
        {
            _serviceRavenDB = serviceRavenDB;
            _localizer = localizer;

            RuleFor(user => user.Account.Email)
                .NotEmpty().WithMessage(_localizer["EmailRequired"])
                .EmailAddress().WithMessage(_localizer["EmailInvalid"])
                .Must((user, email) => !EmailAlreadyExist(email, user.Id)).WithMessage(_localizer["EmailAlreadyExists"]);


            RuleFor(user => user.Profile.Name)
                .NotEmpty().WithMessage(_localizer["NameRequired"])
                .MaximumLength(100).WithMessage(_localizer["NameMaxLength"]);

            RuleFor(user => user.Profile.DateOfBirth)
                .LessThan(DateTime.Now).WithMessage(_localizer["DateOfBirthPast"])
                .GreaterThan(DateTime.Now.AddYears(-120)).WithMessage(_localizer["DateOfBirthTooOld"]);

            RuleFor(user => user.Account.Role)
                .NotEmpty().WithMessage(_localizer["RoleRequired"])
                .Must(role => new[] { "User", "Admin", "Moderator" }.Contains(role))
                .WithMessage(_localizer["RoleInvalid"]);
        }

        private bool EmailAlreadyExist(string email, string? currentUserId)
        {
            return _serviceRavenDB.Session.Query<User>()
                .Any(x => x.Account.Email == email && x.Id != currentUserId);
        }
    }
}
