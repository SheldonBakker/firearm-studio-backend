namespace FirearmStudio.Domain.Authentication;

public static class AppRoles
{
    public const string Admin = "admin";
    public const string Manager = "manager";
    public const string Staff = "staff";
    public const string Viewer = "viewer";

    public static string ToRoleString(this Enums.AppRole role) => role.ToString().ToLowerInvariant();

    public static class Policy
    {
        public const string AdminOnly = Admin;
        public const string ManagerOrAbove = $"{Admin},{Manager}";
        public const string StaffOrAbove = $"{Admin},{Manager},{Staff}";
        public const string AnyAuthenticatedRole = $"{Admin},{Manager},{Staff},{Viewer}";
    }
}
