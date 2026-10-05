using ErrorOr;
using FirearmStudio.Application.Abstractions.Messaging;

namespace FirearmStudio.Application.Companies.GetStorefrontAccess;

public sealed record GetStorefrontAccessQuery : IQuery<ErrorOr<StorefrontKeyResult>>;
