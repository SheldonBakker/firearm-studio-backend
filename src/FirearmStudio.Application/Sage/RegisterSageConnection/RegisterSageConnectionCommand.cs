using ErrorOr;
using FirearmStudio.Application.Abstractions.Messaging;

namespace FirearmStudio.Application.Sage.RegisterSageConnection;

public sealed record RegisterSageConnectionCommand(RegisterSageConnectionRequest Request)
    : ICommand<ErrorOr<SageConnectionResponse>>;
