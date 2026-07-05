using ErrorOr;
using FirearmStudio.Application.Abstractions.Messaging;

namespace FirearmStudio.Application.Packages.CreatePackage;

public sealed record CreatePackageCommand(CreatePackageRequest Request) : ICommand<ErrorOr<PackageResponse>>;
