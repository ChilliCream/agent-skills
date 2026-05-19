namespace Reference.VerticalSlice.Shared.Security;

public static class LibraryPolicies
{
    public const string Read = "Library.Read";

    public const string Write = "Library.Write";

    public static void Configure(Microsoft.AspNetCore.Authorization.AuthorizationOptions options)
    {
        options.AddPolicy(Read, policy => policy.RequireAuthenticatedUser());
        options.AddPolicy(Write, policy => policy.RequireAuthenticatedUser());
    }
}
