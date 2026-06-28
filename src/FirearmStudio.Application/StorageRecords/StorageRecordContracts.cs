using FirearmStudio.Domain.Enums;

namespace FirearmStudio.Application.StorageRecords;

public sealed record StorageRecordDto(
    Guid Id,
    Guid FirearmId,
    Guid CustomerId,
    string? CustomerName,
    string SerialNumber,
    StorageStatus StorageStatus,
    decimal MonthlyRate,
    string? StorageLocation,
    string? RackNumber,
    string? SafeNumber,
    DateOnly StoredFrom,
    DateOnly? StoredUntil);

public sealed record CustomerStorageRecordDto(
    Guid Id,
    Guid FirearmId,
    decimal MonthlyRate,
    StorageStatus StorageStatus,
    DateOnly StoredFrom,
    DateOnly? StoredUntil);

public sealed record StartStorageRequest(
    DateOnly StoredFrom, decimal MonthlyRate, string? StorageLocation, string? RackNumber, string? SafeNumber, string? Notes);

public sealed record ReleaseStorageRequest(DateOnly? StoredUntil);
