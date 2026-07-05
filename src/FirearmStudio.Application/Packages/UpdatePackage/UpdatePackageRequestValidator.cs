using FluentValidation;

namespace FirearmStudio.Application.Packages.UpdatePackage;

public sealed class UpdatePackageRequestValidator : AbstractValidator<UpdatePackageRequest>
{
    public UpdatePackageRequestValidator()
    {
        RuleFor(request => request)
            .Must(request => request.Name.IsSet
                             || request.Description.IsSet
                             || request.Price.IsSet
                             || request.DurationMinutes.IsSet
                             || request.MaxShooters.IsSet
                             || request.IsActive.IsSet
                             || request.Items.IsSet)
            .WithMessage("At least one field must be supplied.");

        RuleFor(request => request.Name.Value)
            .NotEmpty()
            .MaximumLength(200)
            .OverridePropertyName(nameof(UpdatePackageRequest.Name))
            .When(request => request.Name.IsSet);

        RuleFor(request => request.Description.Value)
            .MaximumLength(2000)
            .OverridePropertyName(nameof(UpdatePackageRequest.Description))
            .When(request => request.Description.IsSet);

        RuleFor(request => request.Price.Value)
            .GreaterThanOrEqualTo(0)
            .OverridePropertyName(nameof(UpdatePackageRequest.Price))
            .When(request => request.Price.IsSet);

        RuleFor(request => request.DurationMinutes.Value)
            .InclusiveBetween(15, 480)
            .OverridePropertyName(nameof(UpdatePackageRequest.DurationMinutes))
            .When(request => request.DurationMinutes.IsSet);

        RuleFor(request => request.MaxShooters.Value)
            .InclusiveBetween(1, 20)
            .OverridePropertyName(nameof(UpdatePackageRequest.MaxShooters))
            .When(request => request.MaxShooters.IsSet);

        RuleFor(request => request.Items.Value)
            .NotNull()
            .OverridePropertyName(nameof(UpdatePackageRequest.Items))
            .When(request => request.Items.IsSet);

        RuleForEach(request => request.Items.Value)
            .ChildRules(item =>
            {
                item.RuleFor(i => i.Description).NotEmpty().MaximumLength(300);
                item.RuleFor(i => i.Quantity).GreaterThan(0);
            })
            .OverridePropertyName(nameof(UpdatePackageRequest.Items))
            .When(request => request.Items.IsSet && request.Items.Value is not null);
    }
}
