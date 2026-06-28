using FluentValidation;

namespace FirearmStudio.Application.Invoices.RecordPayment;

public sealed class RecordPaymentRequestValidator : AbstractValidator<RecordPaymentRequest>
{
    public RecordPaymentRequestValidator()
    {
        RuleFor(x => x.Amount).GreaterThan(0).WithMessage("Payment amount must be greater than zero.");
        RuleFor(x => x.Reference).NotEmpty().WithMessage("Reference is required.");
        RuleFor(x => x.Method).IsInEnum().WithMessage("Unknown payment method.");
    }
}
