using ErrorOr;
using FirearmStudio.Application.Abstractions.Messaging;

namespace FirearmStudio.Application.Packages.GetPackage;

public sealed record GetPackageQuery(Guid Id) : IQuery<ErrorOr<PackageResponse>>;
