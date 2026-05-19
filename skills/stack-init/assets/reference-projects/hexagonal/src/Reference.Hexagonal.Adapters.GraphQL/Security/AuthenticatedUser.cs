using System.Security.Claims;

namespace Reference.Hexagonal.Adapters.GraphQL.Security;

internal static class AuthenticatedUser
{
    public static void EnsureAuthenticated(ClaimsPrincipal user)
    {
        if (user.Identity is not { IsAuthenticated: true })
        {
            throw new UnauthorizedAccessException();
        }
    }
}
