using FluentValidation;

namespace FirearmStudio.Application.Invoices.GenerateMonthlyInvoices;

public sealed class GenerateMonthlyInvoicesRequestValidator : AbstractValidator<GenerateMonthlyInvoicesRequest>
{
    public GenerateMonthlyInvoicesRequestValidator()
    {
        RuleFor(request => request.InvoiceMonth)
            .NotEqual(default(DateOnly))
            .WithMessage("InvoiceMonth is required.");
        RuleFor(request => request.VatRate)
            .InclusiveBetween(0m, 100m)
            .WithMessage("VatRate must be between 0 and 100.");
        RuleFor(request => request.DueDays)
            .InclusiveBetween(0, 365)
            .WithMessage("DueDays must be between 0 and 365.");
    }
}
