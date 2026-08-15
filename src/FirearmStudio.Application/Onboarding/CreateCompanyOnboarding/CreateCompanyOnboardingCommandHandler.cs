using ErrorOr;
using FirearmStudio.Application.Abstractions;
using FirearmStudio.Application.Abstractions.Messaging;
using FirearmStudio.Domain.Entities;
using FirearmStudio.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace FirearmStudio.Application.Onboarding.CreateCompanyOnboarding;

public sealed class CreateCompanyOnboardingCommandHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUserService,
    ITenantContext tenant)
    : ICommandHandler<CreateCompanyOnboardingCommand, ErrorOr<CompanyResponse>>
{
    public async Task<ErrorOr<CompanyResponse>> Handle(
        CreateCompanyOnboardingCommand command,
        CancellationToken cancellationToken)
    {
        var user = currentUserService.User;
        if (!user.IsAuthenticated)
        {
            return Error.Unauthorized(ErrorCodes.Unauthorized, "A valid session is required.");
        }

        if (string.IsNullOrWhiteSpace(user.Email))
        {
            return Error.Validation(ErrorCodes.EmailMissing, "The access token does not contain an email address.");
        }

        var email = user.Email.Trim().ToLowerInvariant();

        using (tenant.BeginBypass())
        {
            var alreadyOnboarded = await db.AppUsers
                .IgnoreQueryFilters()
                .AnyAsync(u => u.AuthUserId == user.Id || u.Email == email, cancellationToken);

            if (alreadyOnboarded)
            {
                return Error.Conflict(
                    ErrorCodes.AlreadyOnboarded,
                    "This user already belongs to a company or has a pending invite. Refresh your session to access it.");
            }

            var request = command.Request;
            var company = new Company
            {
                Id = Guid.CreateVersion7(),
                Name = request.Name,
                RegistrationNumber = request.RegistrationNumber,
                VatNumber = request.VatNumber,
                Email = request.Email,
                Phone = request.Phone,
                AddressLine1 = request.AddressLine1,
                AddressLine2 = request.AddressLine2,
                City = request.City,
                Province = request.Province,
                PostalCode = request.PostalCode,
            };

            await db.Companies.AddAsync(company, cancellationToken);
            await db.AppUsers.AddAsync(new AppUser
            {
                CompanyId = company.Id,
                AuthUserId = user.Id,
                Email = email,
                Role = AppRole.Admin,
                IsActive = true,
                LinkedAt = DateTime.UtcNow,
            }, cancellationToken);

            try
            {
                await db.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException)
            {
                return Error.Conflict(ErrorCodes.AlreadyOnboarded, "This user already belongs to a company.");
            }

            return new CompanyResponse(company.Id, company.Name);
        }
    }

    public static class ErrorCodes
    {
        public const string Unauthorized = "CreateCompanyOnboardingCommand.Unauthorized";
        public const string EmailMissing = "CreateCompanyOnboardingCommand.EmailMissing";
        public const string AlreadyOnboarded = "CreateCompanyOnboardingCommand.AlreadyOnboarded";
    }
}
