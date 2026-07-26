using System.Security.Claims;
using FirearmStudio.Application.Abstractions;
using FirearmStudio.Domain.Authentication;
using Microsoft.AspNetCore.Http;

namespace FirearmStudio.Infrastructure.Services;

public sealed class CurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUserService
{
    // Cached per scoped instance - HttpContext.User claims do not change within a request.
    private CurrentUser? _cached;

    public CurrentUser User => _cached ??= Resolve();

    private CurrentUser Resolve()
    {
        var principal = httpContextAccessor.HttpContext?.User;
        if (principal?.Identity is not { IsAuthenticated: true })
        {
            return CurrentUser.Anonymous;
        }

        var subject = principal.FindFirstValue(SupabaseClaimTypes.Subject)
                      ?? principal.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(subject, out var userId))
        {
            return CurrentUser.Anonymous;
        }

        var roles = principal.FindAll(ClaimTypes.Role)
            .Select(c => c.Value)
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        Guid? companyId = Guid.TryParse(
            principal.FindFirstValue(SupabaseClaimTypes.CompanyId), out var parsedCompany)
            ? parsedCompany
            : null;

        return new CurrentUser
        {
            Id = userId,
            Email = principal.FindFirstValue(SupabaseClaimTypes.Email)
                    ?? principal.FindFirstValue(ClaimTypes.Email),
            CompanyId = companyId,
            Roles = roles,
            IsAuthenticated = true,
        };
    }
}
