namespace FactoryFlow.Modules.Identity;

/// <summary>
/// Central role name constants shared across modules.
/// </summary>
public static class AppRoles
{
    public const string User = "User";
    public const string Supervisor = "Supervisor";
    public const string Admin = "Admin";

    public const string SupervisorOrAdmin = $"{Supervisor},{Admin}";
    public const string All = $"{User},{Supervisor},{Admin}";
}

public static class AuthPolicies
{
    public const string TicketsUse = "TicketsUse";
    public const string TicketsManage = "TicketsManage";
}
