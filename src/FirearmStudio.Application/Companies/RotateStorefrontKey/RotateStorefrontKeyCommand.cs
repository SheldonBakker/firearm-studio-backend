using ErrorOr;
using FirearmStudio.Application.Abstractions.Messaging;

namespace FirearmStudio.Application.Companies.RotateStorefrontKey;

public sealed record RotateStorefrontKeyCommand : ICommand<ErrorOr<StorefrontKeyResult>>;
