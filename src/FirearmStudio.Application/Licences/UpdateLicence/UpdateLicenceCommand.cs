using ErrorOr;
using FirearmStudio.Application.Abstractions.Messaging;

namespace FirearmStudio.Application.Licences.UpdateLicence;

public sealed record UpdateLicenceCommand(Guid Id, UpdateLicenceRequest Request) : ICommand<ErrorOr<Updated>>;
