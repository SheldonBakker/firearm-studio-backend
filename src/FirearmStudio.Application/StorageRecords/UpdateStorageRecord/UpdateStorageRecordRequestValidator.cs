using FluentValidation;

namespace FirearmStudio.Application.StorageRecords.UpdateStorageRecord;

public sealed class UpdateStorageRecordRequestValidator : AbstractValidator<UpdateStorageRecordRequest>
{
    public UpdateStorageRecordRequestValidator()
    {
        RuleFor(request => request)
            .Must(request => request.StoredFrom.IsSet
                             || request.StoredUntil.IsSet
                             || request.MonthlyRate.IsSet
                             || request.StorageStatus.IsSet
                             || request.StorageLocation.IsSet
                             || request.RackNumber.IsSet
                             || request.SafeNumber.IsSet
                             || request.Notes.IsSet)
            .WithMessage("At least one field must be supplied.");
        RuleFor(request => request.StoredFrom.Value)
            .NotEqual(default(DateOnly))
            .OverridePropertyName(nameof(UpdateStorageRecordRequest.StoredFrom))
            .When(request => request.StoredFrom.IsSet);
        RuleFor(request => request.MonthlyRate.Value)
            .GreaterThan(0)
            .OverridePropertyName(nameof(UpdateStorageRecordRequest.MonthlyRate))
            .When(request => request.MonthlyRate.IsSet);
        RuleFor(request => request.StorageStatus.Value)
            .IsInEnum()
            .OverridePropertyName(nameof(UpdateStorageRecordRequest.StorageStatus))
            .When(request => request.StorageStatus.IsSet);
        RuleFor(request => request.StorageLocation.Value)
            .MaximumLength(200)
            .OverridePropertyName(nameof(UpdateStorageRecordRequest.StorageLocation))
            .When(request => request.StorageLocation.IsSet);
        RuleFor(request => request.RackNumber.Value)
            .MaximumLength(60)
            .OverridePropertyName(nameof(UpdateStorageRecordRequest.RackNumber))
            .When(request => request.RackNumber.IsSet);
        RuleFor(request => request.SafeNumber.Value)
            .MaximumLength(60)
            .OverridePropertyName(nameof(UpdateStorageRecordRequest.SafeNumber))
            .When(request => request.SafeNumber.IsSet);
        RuleFor(request => request.Notes.Value)
            .MaximumLength(4000)
            .OverridePropertyName(nameof(UpdateStorageRecordRequest.Notes))
            .When(request => request.Notes.IsSet);
        RuleFor(request => request)
            .Must(request => !request.StoredFrom.IsSet
                             || !request.StoredUntil.IsSet
                             || request.StoredUntil.Value is null
                             || request.StoredUntil.Value >= request.StoredFrom.Value)
            .WithMessage("StoredUntil must be on or after StoredFrom.");
    }
}
