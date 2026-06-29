using FluentValidation;

namespace FirearmStudio.Application.Licences.CreateLicence;

public sealed class CreateLicenceRequestValidator : AbstractValidator<CreateLicenceRequest>
{
    public CreateLicenceRequestValidator()
    {
        RuleFor(request => request.LicenceNumber).NotEmpty().MaximumLength(120);
        RuleFor(request => request.ExpiresOn).NotEqual(default(DateOnly));
        RuleFor(request => request)
            .Must(request => request.IssuedOn is null || request.IssuedOn <= request.ExpiresOn)
            .WithMessage("IssuedOn must be on or before ExpiresOn.");
        RuleFor(request => request.DocumentUrl).MaximumLength(2048);
    }
}
