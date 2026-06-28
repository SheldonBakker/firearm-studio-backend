using FirearmStudio.Domain.Enums;

namespace FirearmStudio.Application.StorageRecords;

public sealed record ActiveStorageRecordDto(
    Guid Id,
    Guid FirearmId,
    decimal MonthlyRate,
    string? StorageLocation,
    string? RackNumber,
    string? SafeNumber,
    DateOnly StoredFrom);

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
