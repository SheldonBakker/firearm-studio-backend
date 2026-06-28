using ErrorOr;
using FirearmStudio.Application.Abstractions.Messaging;

namespace FirearmStudio.Application.Companies.UpdateCompany;

public sealed record UpdateCompanyCommand(UpdateCompanyRequest Request) : ICommand<ErrorOr<Updated>>;
