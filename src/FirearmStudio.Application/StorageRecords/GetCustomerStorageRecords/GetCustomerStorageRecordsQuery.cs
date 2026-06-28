using ErrorOr;
using FirearmStudio.Application.Abstractions.Messaging;

namespace FirearmStudio.Application.StorageRecords.GetCustomerStorageRecords;

public sealed record GetCustomerStorageRecordsQuery(Guid CustomerId)
    : IQuery<ErrorOr<IReadOnlyList<CustomerStorageRecordDto>>>;
