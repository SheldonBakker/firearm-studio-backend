using ErrorOr;
using FirearmStudio.Application.Abstractions.Messaging;

namespace FirearmStudio.Application.Accounting.RegisterAccountingConnection;

public sealed record RegisterAccountingConnectionCommand(RegisterAccountingConnectionRequest Request)
    : ICommand<ErrorOr<AccountingConnectionResponse>>;
