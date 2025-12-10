using System.Text.RegularExpressions;
using Application.Domain.Model.Students;
using Application.Domain.Model.ValueObjects;
using FluentValidation;

namespace Application.Domain.Validation.Students
{
    public class StudentValidator : AbstractValidator<Student>
    {
        private static readonly Regex LangRegex = new("^[a-z]{2}(?:_[A-Z]{2})?$", RegexOptions.Compiled);
        private static readonly HashSet<string> TimeZoneIds = TimeZoneInfo.GetSystemTimeZones()
            .Select(tz => tz.Id)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        public StudentValidator()
        {
            RuleFor(student => student.FirstName)
                .NotEmpty().WithMessage("StudentFirstNameRequired")
                .Length(2, 100).WithMessage("StudentFirstNameLength");

            RuleFor(student => student.LastName)
                .NotEmpty().WithMessage("StudentLastNameRequired")
                .Length(2, 100).WithMessage("StudentLastNameLength");

            RuleFor(student => student.Email)
                .EmailAddress().WithMessage("StudentEmailInvalid")
                .When(student => !string.IsNullOrWhiteSpace(student.Email));

            RuleFor(student => student.IdNumber)
                .Cascade(CascadeMode.Stop)
                .Must(id => string.IsNullOrWhiteSpace(id) || id.Trim().Length > 0)
                .WithMessage("StudentIdNumberRequired")
                .MaximumLength(20).WithMessage("StudentIdNumberMaxLength");

            RuleFor(student => student.Lang)
                .Matches(LangRegex).WithMessage("StudentLangInvalid")
                .When(student => !string.IsNullOrWhiteSpace(student.Lang));

            RuleFor(student => student.TimeZone)
                .Must(BeValidTimeZone).WithMessage("StudentTimeZoneInvalid")
                .When(student => !string.IsNullOrWhiteSpace(student.TimeZone));

            When(student => student.Address != null, () =>
            {
                RuleFor(student => student.Address!).SetValidator(new AddressValidator());
            });
        }

        private static bool BeValidTimeZone(string? timeZone)
        {
            if (string.IsNullOrWhiteSpace(timeZone))
            {
                return true;
            }

            if (TimeZoneIds.Contains(timeZone))
            {
                return true;
            }

            return TimeZoneInfo.TryConvertIanaIdToWindowsId(timeZone, out var windowsId) && TimeZoneIds.Contains(windowsId);
        }
    }

    public class AddressValidator : AbstractValidator<Address>
    {
        public AddressValidator()
        {
            RuleFor(address => address.Street).MaximumLength(150).When(address => !string.IsNullOrWhiteSpace(address.Street));
            RuleFor(address => address.Number).MaximumLength(20).When(address => !string.IsNullOrWhiteSpace(address.Number));
            RuleFor(address => address.District).MaximumLength(100).When(address => !string.IsNullOrWhiteSpace(address.District));
            RuleFor(address => address.City).MaximumLength(100).When(address => !string.IsNullOrWhiteSpace(address.City));
            RuleFor(address => address.State).MaximumLength(100).When(address => !string.IsNullOrWhiteSpace(address.State));
            RuleFor(address => address.PostalCode).MaximumLength(20).When(address => !string.IsNullOrWhiteSpace(address.PostalCode));
            RuleFor(address => address.Country).MaximumLength(100).When(address => !string.IsNullOrWhiteSpace(address.Country));
        }
    }
}
