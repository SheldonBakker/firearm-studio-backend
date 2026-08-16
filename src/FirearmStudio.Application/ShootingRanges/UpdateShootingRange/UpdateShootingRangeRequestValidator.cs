using FirearmStudio.Application.Model;
using FluentValidation;

namespace FirearmStudio.Application.ShootingRanges.UpdateShootingRange;

public sealed class UpdateShootingRangeRequestValidator : AbstractValidator<UpdateShootingRangeRequest>
{
    public UpdateShootingRangeRequestValidator()
    {
        RuleFor(request => request)
            .Must(r => OptionalHelpers.HasAtLeastOneSet(r))
            .WithMessage("At least one field must be supplied.");

        RuleFor(request => request.Name.Value)
            .NotEmpty()
            .MaximumLength(200)
            .OverridePropertyName(nameof(UpdateShootingRangeRequest.Name))
            .When(request => request.Name.IsSet);

        RuleFor(request => request.Description.Value)
            .MaximumLength(2000)
            .OverridePropertyName(nameof(UpdateShootingRangeRequest.Description))
            .When(request => request.Description.IsSet);

        RuleFor(request => request.LaneCount.Value)
            .InclusiveBetween(1, 100)
            .OverridePropertyName(nameof(UpdateShootingRangeRequest.LaneCount))
            .When(request => request.LaneCount.IsSet);

        RuleFor(request => request.SlotIntervalMinutes.Value)
            .InclusiveBetween(5, 240)
            .OverridePropertyName(nameof(UpdateShootingRangeRequest.SlotIntervalMinutes))
            .When(request => request.SlotIntervalMinutes.IsSet);

        RuleFor(request => request.OperatingHours.Value)
            .NotNull()
            .Must(hours => hours is null || hours.Select(h => h.Day).Distinct().Count() == hours.Count)
            .WithMessage("Operating hours may only contain one window per day.")
            .OverridePropertyName(nameof(UpdateShootingRangeRequest.OperatingHours))
            .When(request => request.OperatingHours.IsSet);

        RuleForEach(request => request.OperatingHours.Value)
            .ChildRules(hours =>
            {
                hours.RuleFor(h => h.Day).IsInEnum();
                hours.RuleFor(h => h.CloseTime)
                    .GreaterThan(h => h.OpenTime)
                    .WithMessage("Close time must be after open time.");
            })
            .OverridePropertyName(nameof(UpdateShootingRangeRequest.OperatingHours))
            .When(request => request.OperatingHours.IsSet && request.OperatingHours.Value is not null);
    }
}
