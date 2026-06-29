using FluentValidation;

namespace FirearmStudio.Application.StorageRecords.StartStorage;

public sealed class StartStorageRequestValidator : AbstractValidator<StartStorageRequest>
{
    public StartStorageRequestValidator()
    {
        RuleFor(request => request.StoredFrom).NotEqual(default(DateOnly));
        RuleFor(request => request.MonthlyRate).GreaterThan(0);
        RuleFor(request => request.StorageLocation).MaximumLength(200);
        RuleFor(request => request.RackNumber).MaximumLength(60);
        RuleFor(request => request.SafeNumber).MaximumLength(60);
        RuleFor(request => request.Notes).MaximumLength(4000);
    }
}
