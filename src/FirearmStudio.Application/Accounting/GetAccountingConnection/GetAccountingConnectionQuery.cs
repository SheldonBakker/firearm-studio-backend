using ErrorOr;
using FirearmStudio.Application.Abstractions.Messaging;

namespace FirearmStudio.Application.Accounting.GetAccountingConnection;

public sealed record GetAccountingConnectionQuery : IQuery<ErrorOr<AccountingConnectionDetailsResponse?>>;
