using System.Security.Claims;
using System.Text.Json;
using FirearmStudio.Domain.Authentication;
using Microsoft.AspNetCore.Authentication;

namespace FirearmStudio.WebApi.Extensions.Authentication;

public sealed class SupabaseRolesClaimsTransformer : IClaimsTransformation
{
    public Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        if (principal.Identity is not ClaimsIdentity { IsAuthenticated: true } identity)
        {
            return Task.FromResult(principal);
        }

        var appMetadata = identity.FindFirst(SupabaseClaimTypes.AppMetadata)?.Value;
        if (string.IsNullOrWhiteSpace(appMetadata))
        {
            return Task.FromResult(principal);
        }

        foreach (var raw in ExtractRoles(appMetadata))
        {
            if (!AppRoles.IsKnownRole(raw))
            {
                continue;
            }

            var role = raw.Trim().ToLowerInvariant();

            if (!identity.HasClaim(ClaimTypes.Role, role))
            {
                identity.AddClaim(new Claim(ClaimTypes.Role, role));
            }
        }

        return Task.FromResult(principal);
    }

    private static IEnumerable<string> ExtractRoles(string appMetadataJson)
    {
        JsonElement root;
        try
        {
            using var doc = JsonDocument.Parse(appMetadataJson);
            root = doc.RootElement.Clone();
        }
        catch (JsonException)
        {
            yield break;
        }

        if (root.ValueKind != JsonValueKind.Object)
        {
            yield break;
        }

        if (root.TryGetProperty(SupabaseClaimTypes.RolesKey, out var rolesElement)
            && rolesElement.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in rolesElement.EnumerateArray())
            {
                if (item.ValueKind == JsonValueKind.String)
                {
                    var value = item.GetString();
                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        yield return value;
                    }
                }
            }
        }

        if (root.TryGetProperty(SupabaseClaimTypes.RoleKey, out var roleElement)
            && roleElement.ValueKind == JsonValueKind.String)
        {
            var value = roleElement.GetString();
            if (!string.IsNullOrWhiteSpace(value))
            {
                yield return value;
            }
        }
    }
}
