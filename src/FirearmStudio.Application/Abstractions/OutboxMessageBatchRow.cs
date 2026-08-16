namespace FirearmStudio.Application.Abstractions;

public sealed record OutboxMessageBatchRow(Guid Id, string Type, string Payload, int Attempts);
