using ErrorOr;
using FirearmStudio.Application.Abstractions;
using FirearmStudio.Application.Onboarding;
using FirearmStudio.Domain.Entities;
using FirearmStudio.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace FirearmStudio.Infrastructure.Services;

public sealed class OnboardingService(
    IApplicationDbContext db,
    ICurrentUserService currentUserService,
    ITenantContext tenant) : IOnboardingService
{
    public async Task<ErrorOr<CompanyResponse>> CreateCompanyAsync(
        CreateCompanyRequest request, CancellationToken ct = default)
    {
        var user = currentUserService.User;
        if (!user.IsAuthenticated)
        {
            return Error.Unauthorized(description: "A valid Supabase session is required.");
        }

        if (string.IsNullOrWhiteSpace(user.Email))
        {
            return Error.Validation("User.Email", "The Supabase token does not contain an email address.");
        }

        var email = user.Email.ToLowerInvariant();

        using (tenant.BeginBypass())
        {
            var alreadyOnboarded = await db.AppUsers
                .IgnoreQueryFilters()
                .AnyAsync(u => u.AuthUserId == user.Id || u.Email == email, ct);

            if (alreadyOnboarded)
            {
                return Error.Conflict(
                    description: "This user already belongs to a company or has a pending invite. " +
                                 "Refresh your session to access it.");
            }

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
            await db.Companies.AddAsync(company, ct);

            await db.AppUsers.AddAsync(new AppUser
            {
                CompanyId = company.Id,
                AuthUserId = user.Id,
                Email = email,
                Role = AppRole.Admin,
                IsActive = true,
                LinkedAt = DateTime.UtcNow,
            }, ct);

            try
            {
                await db.SaveChangesAsync(ct);
            }
            catch (DbUpdateException)
            {
                return Error.Conflict(description: "This user already belongs to a company.");
            }

            return new CompanyResponse(company.Id, company.Name);
        }
    }
}
