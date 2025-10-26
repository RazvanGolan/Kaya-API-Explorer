using System.Reflection;
using Microsoft.AspNetCore.Authorization;

namespace Kaya.ApiExplorer.Helpers;

public static class AuthorizationHelper
{
    public static (bool RequiresAuth, List<string> Roles) GetAuthorizationInfo(MemberInfo? member, Type? fallbackType = null)
    {
        if (member is null) return (false, []);
        
        var roles = new List<string>();
        var requiresAuth = false;
        
        var allowAnonymous = member.GetCustomAttribute<AllowAnonymousAttribute>();
        if (allowAnonymous is not null)
        {
            return (false, []);
        }
        
        var authorizeAttr = member.GetCustomAttribute<AuthorizeAttribute>();
        if (authorizeAttr is not null)
        {
            requiresAuth = true;
            if (!string.IsNullOrEmpty(authorizeAttr.Roles))
            {
                roles.AddRange(authorizeAttr.Roles.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(r => r.Trim()));
            }
        }
        
        if (!requiresAuth && fallbackType is not null)
        {
            var controllerAuthorize = fallbackType.GetCustomAttribute<AuthorizeAttribute>();
            if (controllerAuthorize is not null)
            {
                requiresAuth = true;
                if (!string.IsNullOrEmpty(controllerAuthorize.Roles))
                {
                    roles.AddRange(controllerAuthorize.Roles.Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(r => r.Trim()));
                }
            }
        }
        
        return (requiresAuth, roles.Distinct().ToList());
    }
}
