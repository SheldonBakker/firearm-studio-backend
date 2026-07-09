using ErrorOr;
using FirearmStudio.Application.Abstractions.Messaging;

namespace FirearmStudio.Application.Sage.GetSageConnection;

public sealed record GetSageConnectionQuery : IQuery<ErrorOr<SageConnectionDetailsResponse>>;
