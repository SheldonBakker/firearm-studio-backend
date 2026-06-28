using ErrorOr;
using FirearmStudio.Application.Abstractions.Messaging;

namespace FirearmStudio.Application.Companies.GetCompany;

public sealed record GetCompanyQuery : IQuery<ErrorOr<CompanyDetailsResponse>>;
