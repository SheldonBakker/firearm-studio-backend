using FluentValidation;

namespace FirearmStudio.Application.ShootingRanges.CreateShootingRange;

public sealed class CreateShootingRangeRequestValidator : AbstractValidator<CreateShootingRangeRequest>
{
    public CreateShootingRangeRequestValidator()
    {
        RuleFor(request => request.Name).NotEmpty().MaximumLength(200);
        RuleFor(request => request.Description).MaximumLength(2000);
        RuleFor(request => request.LaneCount).InclusiveBetween(1, 100);
        RuleFor(request => request.SlotIntervalMinutes).InclusiveBetween(5, 240);

        RuleFor(request => request.OperatingHours)
            .NotNull()
            .Must(hours => hours is null || hours.Select(h => h.Day).Distinct().Count() == hours.Count)
            .WithMessage("Operating hours may only contain one window per day.");

        RuleForEach(request => request.OperatingHours).ChildRules(hours =>
        {
            hours.RuleFor(h => h.Day).IsInEnum();
            hours.RuleFor(h => h.CloseTime)
                .GreaterThan(h => h.OpenTime)
                .WithMessage("Close time must be after open time.");
        });
    }
}
