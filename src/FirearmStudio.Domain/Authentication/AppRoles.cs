namespace FirearmStudio.Domain.Authentication;

public static class AppRoles
{
    public const string Admin = "admin";
    public const string Manager = "manager";
    public const string Staff = "staff";
    public const string Viewer = "viewer";

    public static readonly IReadOnlyList<string> All = [Admin, Manager, Staff, Viewer];

    public static bool IsKnownRole(string? role) =>
        role is not null && All.Contains(role, StringComparer.OrdinalIgnoreCase);

    public static string ToRoleString(this Enums.AppRole role) => role.ToString().ToLowerInvariant();

    public static bool TryParseRole(string? value, out Enums.AppRole role) =>
        Enum.TryParse(value, ignoreCase: true, out role) && IsKnownRole(value);

    public static class Policy
    {
        public const string ManagerOrAbove = $"{Admin},{Manager}";
        public const string StaffOrAbove = $"{Admin},{Manager},{Staff}";
        public const string AnyAuthenticatedRole = $"{Admin},{Manager},{Staff},{Viewer}";
    }
}
