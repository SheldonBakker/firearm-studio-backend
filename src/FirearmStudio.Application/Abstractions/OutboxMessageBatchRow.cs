namespace FirearmStudio.Application.Abstractions;

// Raw SQL result type for IApplicationDbContext.ClaimOutboxBatchAsync.
// Defined here so WebApi can reference it via IApplicationDbContext without
// depending on Infrastructure directly.
public sealed record OutboxMessageBatchRow(Guid Id, string Type, string Payload, int Attempts);
