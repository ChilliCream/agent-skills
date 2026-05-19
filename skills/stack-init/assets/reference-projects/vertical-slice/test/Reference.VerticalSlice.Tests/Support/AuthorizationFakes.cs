using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace Reference.VerticalSlice.Tests.Support;

public sealed class TestAuthorizationService(bool allow = true) : IAuthorizationService
{
    public Task<AuthorizationResult> AuthorizeAsync(
        ClaimsPrincipal user,
        object? resource,
        IEnumerable<IAuthorizationRequirement> requirements)
    {
        return Task.FromResult(allow ? AuthorizationResult.Success() : AuthorizationResult.Failed());
    }

    public Task<AuthorizationResult> AuthorizeAsync(
        ClaimsPrincipal user,
        object? resource,
        string policyName)
    {
        return Task.FromResult(allow ? AuthorizationResult.Success() : AuthorizationResult.Failed());
    }
}
