using Application.Domain.Model.Dtos;
using FluentValidation;
using Microsoft.Extensions.Localization;

namespace Application.Domain.Validator
{
    public class PasswordValidator : AbstractValidator<PasswordInput>
    {
        public PasswordValidator(IStringLocalizer<SharedResource> localizer)
        {
            RuleFor(x => x.Password)
                .NotEmpty().WithMessage(localizer["PasswordRequired"])
                .MinimumLength(8).WithMessage(localizer["PasswordMinLength"])
                .Matches(@"[A-Z]").WithMessage(localizer["PasswordUppercase"])
                .Matches(@"[a-z]").WithMessage(localizer["PasswordLowercase"])
                .Matches(@"[0-9]").WithMessage(localizer["PasswordNumber"])
                .Matches(@"[!@#$%^&*()_+\-=\[\]{};':""\\|,.<>\/?~`]").WithMessage(localizer["PasswordSpecial"]);
        }
    }
}
