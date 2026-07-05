using ErrorOr;
using FirearmStudio.Application.Abstractions.Messaging;

namespace FirearmStudio.Application.Packages.UpdatePackage;

public sealed record UpdatePackageCommand(Guid Id, UpdatePackageRequest Request) : ICommand<ErrorOr<Updated>>;
