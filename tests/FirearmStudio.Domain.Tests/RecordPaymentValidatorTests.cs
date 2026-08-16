using FirearmStudio.Application.Invoices;
using FirearmStudio.Application.Invoices.RecordPayment;
using FirearmStudio.Domain.Enums;
using Xunit;

namespace FirearmStudio.Domain.Tests;

public class RecordPaymentValidatorTests
{
    private static readonly RecordPaymentRequestValidator Validator = new();

    private static RecordPaymentRequest ValidRequest(string? reference = "REF-001") => new(
        Amount: 100m,
        PaidOn: new DateOnly(2026, 8, 1),
        Method: PaymentMethod.Eft,
        Reference: reference,
        Notes: null);

    private static List<string> ReferenceErrors(RecordPaymentRequest request) =>
        Validator.Validate(request).Errors
            .Where(e => e.PropertyName == nameof(RecordPaymentRequest.Reference))
            .Select(e => e.ErrorMessage)
            .ToList();

    [Fact]
    public void Empty_reference_reports_required_message()
    {
        var messages = ReferenceErrors(ValidRequest(reference: ""));

        Assert.Contains("Reference is required.", messages);
    }

    [Fact]
    public void Over_length_reference_does_not_report_required_message()
    {
        var tooLong = new string('x', 121);
        var messages = ReferenceErrors(ValidRequest(reference: tooLong));

        Assert.DoesNotContain("Reference is required.", messages);
    }

    [Fact]
    public void Over_length_reference_reports_max_length_message()
    {
        var tooLong = new string('x', 121);
        var messages = ReferenceErrors(ValidRequest(reference: tooLong));

        Assert.Contains("Reference must be 120 characters or fewer.", messages);
    }

    [Fact]
    public void Valid_reference_passes_with_no_errors()
    {
        var messages = ReferenceErrors(ValidRequest());

        Assert.Empty(messages);
    }
}
