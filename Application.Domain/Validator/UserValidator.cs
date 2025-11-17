using Application.Domain.Interface;
using Application.Domain.Model.User;
using FluentValidation;

namespace Application.Domain.Validator
{
    public class UserValidator : AbstractValidator<User>
    {
        private readonly IServiceRavenDB _serviceRavenDB;

        public UserValidator(IServiceRavenDB serviceRavenDB)
        {
            _serviceRavenDB = serviceRavenDB;

            RuleFor(user => user.Account.Email)
                .NotEmpty().WithMessage("O email é obrigatório.")
                .EmailAddress().WithMessage("O email deve ser válido.")
                .Must((user, email) => !EmailAlreadyExist(email, user.Id)).WithMessage("Este e-mail já existe na nossa base de dados");

            RuleFor(user => user.Account.Password)
                .NotEmpty().WithMessage("A senha é obrigatória.")
                .MinimumLength(8).WithMessage("A senha deve ter pelo menos 8 caracteres.")
                .Matches(@"[A-Z]").WithMessage("A senha deve conter pelo menos uma letra maiúscula.")
                .Matches(@"[a-z]").WithMessage("A senha deve conter pelo menos uma letra minúscula.")
                .Matches(@"[0-9]").WithMessage("A senha deve conter pelo menos um número.")
                .Matches(@"[!@#$%^&*()_+\-=\[\]{};':""\\|,.<>\/?~`]").WithMessage("A senha deve conter pelo menos um caractere especial (!@#$%^&*()_+-=[]{};':\"\\|,.<>/?~`).");


            RuleFor(user => user.Profile.Name)
                .NotEmpty().WithMessage("O nome é obrigatório.")
                .MaximumLength(100).WithMessage("O nome não pode ter mais de 100 caracteres.");

            RuleFor(user => user.Profile.DateOfBirth)
                .LessThan(DateTime.Now).WithMessage("A data de nascimento deve ser uma data passada.")
                .GreaterThan(DateTime.Now.AddYears(-120)).WithMessage("A data de nascimento não pode ser superior a 120 anos.");

            RuleFor(user => user.Account.Role)
                .NotEmpty().WithMessage("A função do usuário é obrigatória.")
                .Must(role => new[] { "User", "Admin", "Moderator" }.Contains(role))
                .WithMessage("Função inválida. Use 'User', 'Admin' ou 'Moderator'.");
        }

        private bool EmailAlreadyExist(string email, string? currentUserId)
        {
            return _serviceRavenDB.Session.Query<User>()
                .Any(x => x.Account.Email == email && x.Id != currentUserId);
        }
    }
}

