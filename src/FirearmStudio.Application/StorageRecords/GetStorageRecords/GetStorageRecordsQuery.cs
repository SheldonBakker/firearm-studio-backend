using ErrorOr;
using FirearmStudio.Application.Abstractions.Messaging;
using FirearmStudio.Application.Model;
using FirearmStudio.Domain.Enums;

namespace FirearmStudio.Application.StorageRecords.GetStorageRecords;

public sealed record GetStorageRecordsQuery(
    int PageNumber,
    int PageSize,
    StorageStatus? StorageStatus,
    string? SerialNumber,
    string? CustomerName
) : IQuery<ErrorOr<PaginatedResponse<StorageRecordDto>>>;
