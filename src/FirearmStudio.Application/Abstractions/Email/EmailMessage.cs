namespace FirearmStudio.Application.Abstractions.Email;

public abstract record EmailMessage
{
    public string? IdempotencyKey { get; init; }
}
