using Application.Domain.Model.User;
using FluentValidation;

namespace Application.Domain.Validator
{
    /// <summary>
    /// Domain-only validation rule set. It does not reach external services or localization.
    /// Aggregate-level policies (ex.: e-mail único) devem ficar no Application/Service.
    /// </summary>
    public class UserValidator : AbstractValidator<User>
    {
        public UserValidator()
        {
            RuleFor(user => user.Account.Email)
                .NotEmpty().WithMessage("EmailRequired")
                .EmailAddress().WithMessage("EmailInvalid");

            RuleFor(user => user.Profile.Name)
                .NotEmpty().WithMessage("NameRequired")
                .MaximumLength(100).WithMessage("NameMaxLength");

            RuleFor(user => user.Account.Permissions)
                .NotNull().WithMessage("PermissionsRequired")
                .Must(p => p.Any()).WithMessage("PermissionsRequired")
                .ForEach(rule => rule.NotEmpty().WithMessage("PermissionsInvalid"));
        }
    }
}
