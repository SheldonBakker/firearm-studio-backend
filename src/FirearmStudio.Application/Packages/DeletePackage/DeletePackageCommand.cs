using ErrorOr;
using FirearmStudio.Application.Abstractions.Messaging;

namespace FirearmStudio.Application.Packages.DeletePackage;

public sealed record DeletePackageCommand(Guid Id) : ICommand<ErrorOr<Deleted>>;
