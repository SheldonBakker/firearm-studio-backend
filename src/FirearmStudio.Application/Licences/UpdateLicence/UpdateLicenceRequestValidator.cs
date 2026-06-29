using FluentValidation;

namespace FirearmStudio.Application.Licences.UpdateLicence;

public sealed class UpdateLicenceRequestValidator : AbstractValidator<UpdateLicenceRequest>
{
    public UpdateLicenceRequestValidator()
    {
        RuleFor(request => request)
            .Must(request => request.LicenceNumber.IsSet
                             || request.IssuedOn.IsSet
                             || request.ExpiresOn.IsSet
                             || request.Status.IsSet
                             || request.DocumentUrl.IsSet)
            .WithMessage("At least one field must be supplied.");
        RuleFor(request => request.LicenceNumber.Value)
            .NotEmpty()
            .MaximumLength(120)
            .OverridePropertyName(nameof(UpdateLicenceRequest.LicenceNumber))
            .When(request => request.LicenceNumber.IsSet);
        RuleFor(request => request.ExpiresOn.Value)
            .NotEqual(default(DateOnly))
            .OverridePropertyName(nameof(UpdateLicenceRequest.ExpiresOn))
            .When(request => request.ExpiresOn.IsSet);
        RuleFor(request => request.Status.Value)
            .IsInEnum()
            .OverridePropertyName(nameof(UpdateLicenceRequest.Status))
            .When(request => request.Status.IsSet);
        RuleFor(request => request.DocumentUrl.Value)
            .MaximumLength(2048)
            .OverridePropertyName(nameof(UpdateLicenceRequest.DocumentUrl))
            .When(request => request.DocumentUrl.IsSet);
        RuleFor(request => request)
            .Must(request => !request.IssuedOn.IsSet
                             || !request.ExpiresOn.IsSet
                             || request.IssuedOn.Value is null
                             || request.IssuedOn.Value <= request.ExpiresOn.Value)
            .WithMessage("IssuedOn must be on or before ExpiresOn.");
    }
}
