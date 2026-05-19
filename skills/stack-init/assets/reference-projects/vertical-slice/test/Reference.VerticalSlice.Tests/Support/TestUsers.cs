using System.Security.Claims;

namespace Reference.VerticalSlice.Tests.Support;

public static class TestUsers
{
    public static ClaimsPrincipal Authenticated()
    {
        return new ClaimsPrincipal(
            new ClaimsIdentity(
                [new Claim(ClaimTypes.NameIdentifier, "user-1")],
                authenticationType: "Test"));
    }

    public static ClaimsPrincipal Anonymous() => new(new ClaimsIdentity());
}
