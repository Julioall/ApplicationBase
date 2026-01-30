using Application.Domain.Model.Dtos.Common;
using FluentValidation;

using Application.Domain.Model.Dtos.User;
namespace Application.Domain.Validation.User
{
    /// <summary>
    /// Domain-only password strength rules (sem dependências externas).
    /// </summary>
    public class PasswordValidator : AbstractValidator<PasswordInput>
    {
        public PasswordValidator()
        {
            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("PasswordRequired")
                .MinimumLength(8).WithMessage("PasswordMinLength")
                .Matches(@"[A-Z]").WithMessage("PasswordUppercase")
                .Matches(@"[a-z]").WithMessage("PasswordLowercase")
                .Matches(@"[0-9]").WithMessage("PasswordNumber")
                .Matches(@"[!@#$%^&*()_+\-=\[\]{};':""\\|,.<>\/?~`]").WithMessage("PasswordSpecial");
        }
    }
}



