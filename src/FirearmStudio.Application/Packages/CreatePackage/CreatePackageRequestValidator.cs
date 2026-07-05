using FluentValidation;

namespace FirearmStudio.Application.Packages.CreatePackage;

public sealed class CreatePackageRequestValidator : AbstractValidator<CreatePackageRequest>
{
    public CreatePackageRequestValidator()
    {
        RuleFor(request => request.Name).NotEmpty().MaximumLength(200);
        RuleFor(request => request.Description).MaximumLength(2000);
        RuleFor(request => request.Price).GreaterThanOrEqualTo(0);
        RuleFor(request => request.DurationMinutes).InclusiveBetween(15, 480);
        RuleFor(request => request.MaxShooters).InclusiveBetween(1, 20);
        RuleFor(request => request.Items).NotNull();
        RuleForEach(request => request.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.Description).NotEmpty().MaximumLength(300);
            item.RuleFor(i => i.Quantity).GreaterThan(0);
        });
    }
}
