namespace FirearmStudio.Application.Abstractions.Email;

public sealed record InvoiceEmailLine(
    string Description, decimal Quantity, decimal UnitPrice, decimal LineTotal);
