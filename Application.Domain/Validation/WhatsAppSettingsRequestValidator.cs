using Application.Domain.Model.Dtos;
using FluentValidation;
using Microsoft.Extensions.Localization;

namespace Application.Domain.Validation
{
    public class WhatsAppSettingsRequestValidator : AbstractValidator<WhatsAppSettingsRequest>
    {
        public WhatsAppSettingsRequestValidator(IStringLocalizer<SharedResource> localizer)
        {
            RuleFor(request => request.MaxUserInstances)
                .GreaterThan(0).WithMessage(localizer["WhatsAppMaxUserInstancesInvalid"]);
        }
    }
}
