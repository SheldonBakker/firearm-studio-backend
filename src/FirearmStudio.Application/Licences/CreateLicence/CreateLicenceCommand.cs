using ErrorOr;
using FirearmStudio.Application.Abstractions.Messaging;

namespace FirearmStudio.Application.Licences.CreateLicence;

public sealed record CreateLicenceCommand(Guid FirearmId, CreateLicenceRequest Request) : ICommand<ErrorOr<Guid>>;
