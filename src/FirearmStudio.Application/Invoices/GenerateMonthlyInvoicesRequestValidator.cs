using FluentValidation;

namespace FirearmStudio.Application.Invoices;

public sealed class GenerateMonthlyInvoicesRequestValidator : AbstractValidator<GenerateMonthlyInvoicesRequest>
{
    public GenerateMonthlyInvoicesRequestValidator()
    {
        RuleFor(x => x.InvoiceMonth)
            .NotEqual(default(DateOnly))
            .WithMessage("InvoiceMonth is required.");

        RuleFor(x => x.VatRate)
            .InclusiveBetween(0m, 100m)
            .WithMessage("VatRate must be between 0 and 100.");

        RuleFor(x => x.DueDays)
            .InclusiveBetween(0, 365)
            .WithMessage("DueDays must be between 0 and 365.");
    }
}
