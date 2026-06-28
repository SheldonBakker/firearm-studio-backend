using ErrorOr;
using FirearmStudio.Application.Abstractions.Messaging;

namespace FirearmStudio.Application.Onboarding.CreateCompanyOnboarding;

public sealed record CreateCompanyOnboardingCommand(CreateCompanyRequest Request) : ICommand<ErrorOr<CompanyResponse>>;
