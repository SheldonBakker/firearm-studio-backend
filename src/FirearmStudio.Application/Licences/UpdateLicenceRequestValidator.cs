using FluentValidation;

namespace FirearmStudio.Application.Licences;

public sealed class UpdateLicenceRequestValidator : AbstractValidator<UpdateLicenceRequest>
{
    public UpdateLicenceRequestValidator()
    {
        RuleFor(x => x.LicenceNumber.Value)
            .NotEmpty()
            .OverridePropertyName(nameof(UpdateLicenceRequest.LicenceNumber))
            .When(x => x.LicenceNumber.IsSet);
    }
}
