using System.Linq.Expressions;
using FirearmStudio.Application.Model;
using FirearmStudio.Domain.Entities;
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
    DateOnly? StoredUntil)
{
    public static Expression<Func<StorageRecord, StorageRecordDto>> QueryProjection => record =>
        new StorageRecordDto(
            record.Id,
            record.FirearmId,
            record.Firearm!.CustomerId,
            record.Firearm.Customer!.FullName ?? record.Firearm.Customer.CompanyName,
            record.Firearm.SerialNumber,
            record.StorageStatus,
            record.MonthlyRate,
            record.StorageLocation,
            record.RackNumber,
            record.SafeNumber,
            record.StoredFrom,
            record.StoredUntil);
}

public sealed record CustomerStorageRecordDto(
    Guid Id,
    Guid FirearmId,
    decimal MonthlyRate,
    StorageStatus StorageStatus,
    DateOnly StoredFrom,
    DateOnly? StoredUntil)
{
    public static Expression<Func<StorageRecord, CustomerStorageRecordDto>> QueryProjection => record =>
        new CustomerStorageRecordDto(
            record.Id,
            record.FirearmId,
            record.MonthlyRate,
            record.StorageStatus,
            record.StoredFrom,
            record.StoredUntil);
}

public sealed record StartStorageRequest(
    DateOnly StoredFrom, decimal MonthlyRate, string? StorageLocation, string? RackNumber, string? SafeNumber, string? Notes);

public sealed record CreateStorageResponse(Guid Id);

public sealed record UpdateStorageRecordRequest(
    Optional<DateOnly> StoredFrom,
    Optional<DateOnly?> StoredUntil,
    Optional<decimal> MonthlyRate,
    Optional<StorageStatus> StorageStatus,
    Optional<string?> StorageLocation,
    Optional<string?> RackNumber,
    Optional<string?> SafeNumber,
    Optional<string?> Notes);
