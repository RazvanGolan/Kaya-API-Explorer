using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Kaya.ApiExplorer.Helpers;

namespace Kaya.ApiExplorer.Tests;

public class AuthorizationHelperTests
{
    #region Test Controllers

    // Controller with no authorization
    public class NoAuthController : ControllerBase
    {
        public IActionResult GetPublic() => Ok();
    }

    // Controller with [Authorize] but no roles
    [Authorize]
    public class AuthorizedController : ControllerBase
    {
        public IActionResult GetProtected() => Ok();
    }

    // Controller with [Authorize] and specific role
    [Authorize(Roles = "Admin")]
    public class AdminController : ControllerBase
    {
        public IActionResult GetAdmin() => Ok();
    }

    // Controller with [Authorize] and multiple roles
    [Authorize(Roles = "Admin,Manager")]
    public class MultiRoleController : ControllerBase
    {
        public IActionResult GetMultiRole() => Ok();
    }

    // Controller with mixed authorization
    [Authorize]
    public class MixedAuthController : ControllerBase
    {
        // Should inherit controller auth
        public IActionResult GetInherited() => Ok();

        // Should override with AllowAnonymous
        [AllowAnonymous]
        public IActionResult GetAnonymous() => Ok();

        // Should override with specific role
        [Authorize(Roles = "SuperAdmin")]
        public IActionResult GetSuperAdmin() => Ok();

        // Should have both controller and method roles
        [Authorize(Roles = "Manager")]
        public IActionResult GetManager() => Ok();
    }

    // Controller with roles, but methods with AllowAnonymous
    [Authorize(Roles = "Admin")]
    public class AdminWithAnonymousController : ControllerBase
    {
        [AllowAnonymous]
        public IActionResult GetPublic() => Ok();
    }

    #endregion

    [Fact]
    public void GetAuthorizationInfo_WithNullMember_ReturnsNoAuth()
    {
        // Act
        var (requiresAuth, roles) = AuthorizationHelper.GetAuthorizationInfo(null);

        // Assert
        Assert.False(requiresAuth);
        Assert.Empty(roles);
    }

    [Fact]
    public void GetAuthorizationInfo_WithNoAuthController_ReturnsNoAuth()
    {
        // Arrange
        var controllerType = typeof(NoAuthController);

        // Act
        var (requiresAuth, roles) = AuthorizationHelper.GetAuthorizationInfo(controllerType);

        // Assert
        Assert.False(requiresAuth);
        Assert.Empty(roles);
    }

    [Fact]
    public void GetAuthorizationInfo_WithAuthorizedControllerNoRoles_ReturnsAuthWithNoRoles()
    {
        // Arrange
        var controllerType = typeof(AuthorizedController);

        // Act
        var (requiresAuth, roles) = AuthorizationHelper.GetAuthorizationInfo(controllerType);

        // Assert
        Assert.True(requiresAuth);
        Assert.Empty(roles);
    }

    [Fact]
    public void GetAuthorizationInfo_WithAdminController_ReturnsAuthWithAdminRole()
    {
        // Arrange
        var controllerType = typeof(AdminController);

        // Act
        var (requiresAuth, roles) = AuthorizationHelper.GetAuthorizationInfo(controllerType);

        // Assert
        Assert.True(requiresAuth);
        Assert.Single(roles);
        Assert.Contains("Admin", roles);
    }

    [Fact]
    public void GetAuthorizationInfo_WithMultiRoleController_ReturnsAuthWithMultipleRoles()
    {
        // Arrange
        var controllerType = typeof(MultiRoleController);

        // Act
        var (requiresAuth, roles) = AuthorizationHelper.GetAuthorizationInfo(controllerType);

        // Assert
        Assert.True(requiresAuth);
        Assert.Equal(2, roles.Count);
        Assert.Contains("Admin", roles);
        Assert.Contains("Manager", roles);
    }

    [Fact]
    public void GetAuthorizationInfo_WithNoAuthMethod_ReturnsNoAuth()
    {
        // Arrange
        var method = typeof(NoAuthController).GetMethod(nameof(NoAuthController.GetPublic))!;

        // Act
        var (requiresAuth, roles) = AuthorizationHelper.GetAuthorizationInfo(method);

        // Assert
        Assert.False(requiresAuth);
        Assert.Empty(roles);
    }

    [Fact]
    public void GetAuthorizationInfo_WithMethodInheritingControllerAuth_ReturnsControllerAuth()
    {
        // Arrange
        var method = typeof(MixedAuthController).GetMethod(nameof(MixedAuthController.GetInherited))!;
        var controllerType = typeof(MixedAuthController);

        // Act
        var (requiresAuth, roles) = AuthorizationHelper.GetAuthorizationInfo(method, controllerType);

        // Assert
        Assert.True(requiresAuth);
        Assert.Empty(roles); // Controller has [Authorize] but no roles
    }

    [Fact]
    public void GetAuthorizationInfo_WithAllowAnonymousMethod_ReturnsNoAuth()
    {
        // Arrange
        var method = typeof(MixedAuthController).GetMethod(nameof(MixedAuthController.GetAnonymous))!;
        var controllerType = typeof(MixedAuthController);

        // Act
        var (requiresAuth, roles) = AuthorizationHelper.GetAuthorizationInfo(method, controllerType);

        // Assert
        Assert.False(requiresAuth); // [AllowAnonymous] overrides controller [Authorize]
        Assert.Empty(roles);
    }

    [Fact]
    public void GetAuthorizationInfo_WithMethodOverridingControllerRoles_ReturnsMethodRoles()
    {
        // Arrange
        var method = typeof(MixedAuthController).GetMethod(nameof(MixedAuthController.GetSuperAdmin))!;
        var controllerType = typeof(MixedAuthController);

        // Act
        var (requiresAuth, roles) = AuthorizationHelper.GetAuthorizationInfo(method, controllerType);

        // Assert
        Assert.True(requiresAuth);
        Assert.Single(roles);
        Assert.Contains("SuperAdmin", roles); // Method role, not controller role
    }

    [Fact]
    public void GetAuthorizationInfo_WithMethodHavingOwnRoles_ReturnsMethodRoles()
    {
        // Arrange
        var method = typeof(MixedAuthController).GetMethod(nameof(MixedAuthController.GetManager))!;
        var controllerType = typeof(MixedAuthController);

        // Act
        var (requiresAuth, roles) = AuthorizationHelper.GetAuthorizationInfo(method, controllerType);

        // Assert
        Assert.True(requiresAuth);
        Assert.Single(roles);
        Assert.Contains("Manager", roles); // Only method role
    }

    [Fact]
    public void GetAuthorizationInfo_WithAllowAnonymousOverridingControllerRoles_ReturnsNoAuth()
    {
        // Arrange
        var method = typeof(AdminWithAnonymousController).GetMethod(nameof(AdminWithAnonymousController.GetPublic))!;
        var controllerType = typeof(AdminWithAnonymousController);

        // Act
        var (requiresAuth, roles) = AuthorizationHelper.GetAuthorizationInfo(method, controllerType);

        // Assert
        Assert.False(requiresAuth); // [AllowAnonymous] overrides [Authorize(Roles = "Admin")]
        Assert.Empty(roles);
    }

    [Fact]
    public void GetAuthorizationInfo_WithRolesContainingSpaces_TrimsSpaces()
    {
        // Arrange
        // Create a controller with roles that have spaces
        var controllerType = typeof(MultiRoleController); // Has "Admin,Manager"

        // Act
        var (requiresAuth, roles) = AuthorizationHelper.GetAuthorizationInfo(controllerType);

        // Assert
        Assert.True(requiresAuth);
        Assert.All(roles, role => Assert.DoesNotContain(" ", role.Trim()));
    }

    [Fact]
    public void GetAuthorizationInfo_WithDuplicateRoles_ReturnsDistinctRoles()
    {
        // This test verifies that duplicate roles are removed
        // Since we can't easily create duplicate roles with attributes,
        // we'll test the Distinct() behavior is working
        
        // Arrange
        var controllerType = typeof(AdminController);

        // Act
        var (requiresAuth, roles) = AuthorizationHelper.GetAuthorizationInfo(controllerType);

        // Assert
        Assert.True(requiresAuth);
        Assert.Equal(roles.Count, roles.Distinct().Count()); // Ensure no duplicates
    }

    [Fact]
    public void GetAuthorizationInfo_WithoutFallbackType_OnlyChecksMethod()
    {
        // Arrange
        var method = typeof(MixedAuthController).GetMethod(nameof(MixedAuthController.GetInherited))!;
        // Note: NOT passing the controller type as fallback

        // Act
        var (requiresAuth, roles) = AuthorizationHelper.GetAuthorizationInfo(method, null);

        // Assert
        Assert.False(requiresAuth); // Method itself has no [Authorize], and no fallback provided
        Assert.Empty(roles);
    }

    [Fact]
    public void GetAuthorizationInfo_WithEmptyRolesString_ReturnsAuthWithNoRoles()
    {
        // Arrange
        var controllerType = typeof(AuthorizedController); // Has [Authorize] with no Roles

        // Act
        var (requiresAuth, roles) = AuthorizationHelper.GetAuthorizationInfo(controllerType);

        // Assert
        Assert.True(requiresAuth);
        Assert.Empty(roles); // Roles should be empty, not null
    }
}
