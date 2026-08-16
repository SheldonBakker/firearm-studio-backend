using FluentValidation;

namespace FirearmStudio.Application.Invoices.RecordPayment;

public sealed class RecordPaymentRequestValidator : AbstractValidator<RecordPaymentRequest>
{
    public RecordPaymentRequestValidator()
    {
        RuleFor(request => request.Amount).GreaterThan(0).WithMessage("Payment amount must be greater than zero.");
        RuleFor(request => request.PaidOn)
            .Must(date => date is null || date.Value != default)
            .WithMessage("PaidOn must be a valid date.");
        RuleFor(request => request.Reference).NotEmpty().WithMessage("Reference is required.").MaximumLength(120).WithMessage("Reference must be 120 characters or fewer.");
        RuleFor(request => request.Notes).MaximumLength(4000);
        RuleFor(request => request.Method).IsInEnum().WithMessage("Unknown payment method.");
    }
}
