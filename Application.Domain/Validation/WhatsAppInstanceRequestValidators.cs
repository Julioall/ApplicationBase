using Application.Domain.Localization;
using Application.Domain.Model.Dtos;
using FluentValidation;
using Microsoft.Extensions.Localization;

namespace Application.Domain.Validation
{
    public class CreateWhatsAppInstanceRequestValidator : AbstractValidator<CreateWhatsAppInstanceRequest>
    {
        public CreateWhatsAppInstanceRequestValidator(IStringLocalizer<SharedResource> localizer)
        {
            RuleFor(request => request.DisplayName)
                .NotEmpty().WithMessage(localizer["WhatsAppInstanceNameRequired"])
                .MaximumLength(80).WithMessage(localizer["WhatsAppInstanceNameTooLong"]);

            RuleFor(request => request.PhoneNumber)
                .NotEmpty().WithMessage(localizer["WhatsAppPhoneRequired"])
                .Matches(@"^\+?[1-9]\d{9,14}$").WithMessage(localizer["WhatsAppPhoneInvalid"]);
        }
    }

    public class UpdateWhatsAppInstanceRequestValidator : AbstractValidator<UpdateWhatsAppInstanceRequest>
    {
        public UpdateWhatsAppInstanceRequestValidator(IStringLocalizer<SharedResource> localizer)
        {
            RuleFor(request => request.DisplayName)
                .NotEmpty().WithMessage(localizer["WhatsAppInstanceNameRequired"])
                .MaximumLength(80).WithMessage(localizer["WhatsAppInstanceNameTooLong"]);
        }
    }
}
