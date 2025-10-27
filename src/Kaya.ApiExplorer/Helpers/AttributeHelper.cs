using System.Reflection;
using Microsoft.AspNetCore.Authorization;

namespace Kaya.ApiExplorer.Helpers;

/// <summary>
/// Helper class for extracting attribute information from controllers and endpoints.
/// Handles authorization, obsolete, and other metadata attributes.
/// </summary>
public static class AttributeHelper
{
    /// <summary>
    /// Gets authorization information for a member (method or type).
    /// Checks for [Authorize] and [AllowAnonymous] attributes and extracts role requirements.
    /// </summary>
    /// <param name="member">The member to check (method or type)</param>
    /// <param name="fallbackType">Optional fallback type to check if member doesn't have authorization</param>
    /// <returns>Tuple indicating if authorization is required and list of required roles</returns>
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
    
    /// <summary>
    /// Gets obsolete information for a member (method or type).
    /// Checks for [Obsolete] attribute and extracts the message.
    /// </summary>
    /// <param name="member">The member to check (method or type)</param>
    /// <returns>Tuple indicating if member is obsolete and the obsolete message</returns>
    public static (bool IsObsolete, string? Message) GetObsoleteInfo(MemberInfo? member)
    {
        if (member == null) return (false, null);
        
        var obsoleteAttr = member.GetCustomAttribute<ObsoleteAttribute>();
        if (obsoleteAttr != null)
        {
            return (true, obsoleteAttr.Message);
        }
        
        return (false, null);
    }
}
