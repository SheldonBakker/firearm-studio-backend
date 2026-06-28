using ErrorOr;
using FirearmStudio.Application.Abstractions.Messaging;

namespace FirearmStudio.Application.Onboarding.CreateCompanyOnboarding;

public sealed class CreateCompanyOnboardingCommandHandler(IOnboardingService onboardingService)
    : ICommandHandler<CreateCompanyOnboardingCommand, ErrorOr<CompanyResponse>>
{
    public async Task<ErrorOr<CompanyResponse>> Handle(CreateCompanyOnboardingCommand command, CancellationToken cancellationToken) =>
        await onboardingService.CreateCompanyAsync(command.Request, cancellationToken);
}
