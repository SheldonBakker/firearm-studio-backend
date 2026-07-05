using ErrorOr;
using FirearmStudio.Application.Abstractions.Messaging;

namespace FirearmStudio.Application.Packages.GetPublicPackages;

public sealed record GetPublicPackagesQuery(Guid CompanyId) : IQuery<ErrorOr<IReadOnlyList<PublicPackageResponse>>>;
