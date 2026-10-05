using ErrorOr;
using FirearmStudio.Application.Abstractions.Messaging;

namespace FirearmStudio.Application.Companies.RevokeStorefrontKey;

public sealed record RevokeStorefrontKeyCommand : ICommand<ErrorOr<Deleted>>;
