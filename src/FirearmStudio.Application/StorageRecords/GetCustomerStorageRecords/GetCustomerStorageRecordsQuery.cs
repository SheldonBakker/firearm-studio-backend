using ErrorOr;
using FirearmStudio.Application.Abstractions.Messaging;
using FirearmStudio.Application.Model;

namespace FirearmStudio.Application.StorageRecords.GetCustomerStorageRecords;

public sealed record GetCustomerStorageRecordsQuery(
    Guid CustomerId,
    int PageNumber,
    int PageSize
) : IQuery<ErrorOr<PaginatedResponse<CustomerStorageRecordDto>>>;
