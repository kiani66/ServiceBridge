using System.Security.Claims;
using ServiceBridge.Shared.Constants;

namespace ServiceBridge.Web.Navigation;

public static class RoleNavigation
{
    public static IReadOnlyList<NavigationItem> GetItems(ClaimsPrincipal user)
    {
        if (user.Identity?.IsAuthenticated != true)
        {
            return Array.Empty<NavigationItem>();
        }

        if (user.IsInRole(AppConstants.Roles.Admin))
        {
            return
            [
                new("Dashboard", "/ui/admin"),
                new("Catalog", "/Admin/Catalog"),
                new("Pricing", "/Admin/Pricing"),
                new("Users", "/Admin/Users"),
                new("Orders", "/Admin/Orders"),
                new("Profile", "/ui/profile")
            ];
        }

        if (user.IsInRole(AppConstants.Roles.Agent))
        {
            return
            [
                new("Assigned Orders", "/Agent/Orders?status=Assigned"),
                new("Processing", "/Agent/Orders?status=Processing"),
                new("Completed", "/Agent/Orders?status=Completed"),
                new("Profile", "/ui/profile")
            ];
        }

        if (user.IsInRole(AppConstants.Roles.User))
        {
            return
            [
                new("Services", "/ui/catalog"),
                new("Orders", "/ui/orders"),
                new("Profile", "/ui/profile")
            ];
        }

        return Array.Empty<NavigationItem>();
    }

    public static string? GetLandingPage(ClaimsPrincipal user)
    {
        if (user.IsInRole(AppConstants.Roles.Admin))
        {
            return "/ui/admin";
        }

        if (user.IsInRole(AppConstants.Roles.Agent))
        {
            return "/Agent/Orders";
        }

        if (user.IsInRole(AppConstants.Roles.User))
        {
            return "/ui/orders";
        }

        return null;
    }

    public static string? GetLandingPage(string role)
    {
        return role switch
        {
            AppConstants.Roles.Admin => "/ui/admin",
            AppConstants.Roles.Agent => "/Agent/Orders",
            AppConstants.Roles.User => "/ui/orders",
            _ => null
        };
    }
}

public sealed record NavigationItem(string Text, string Page);
