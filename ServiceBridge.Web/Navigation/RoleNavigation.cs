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
                new("Dashboard", "/Dashboard"),
                new("Assignment Queue", "/Admin/Orders/Index"),
                new("Users", "/Admin/Users/Index"),
                new("Catalog", "/Admin/Catalog/Index"),
                new("Pricing", "/Admin/Pricing/Index"),
                new("Profile", "/User/UserProfile")
            ];
        }

        if (user.IsInRole(AppConstants.Roles.Agent))
        {
            return
            [
                new("Dashboard", "/ui/agent"),
                new("Profile", "/User/UserProfile")
            ];
        }

        if (user.IsInRole(AppConstants.Roles.User))
        {
            return
            [
                new("Services", "/Services/Index"),
                new("Orders", "/Orders/Index"),
                new("Profile", "/User/UserProfile")
            ];
        }

        return Array.Empty<NavigationItem>();
    }

    public static string? GetLandingPage(ClaimsPrincipal user)
    {
        if (user.IsInRole(AppConstants.Roles.Admin))
        {
            return "/Dashboard";
        }

        if (user.IsInRole(AppConstants.Roles.Agent))
        {
            return "/ui/agent";
        }

        if (user.IsInRole(AppConstants.Roles.User))
        {
            return "/Orders/Index";
        }

        return null;
    }

    public static string? GetLandingPage(string role)
    {
        return role switch
        {
            AppConstants.Roles.Admin => "/Dashboard",
            AppConstants.Roles.Agent => "/ui/agent",
            AppConstants.Roles.User => "/Orders/Index",
            _ => null
        };
    }
}

public sealed record NavigationItem(string Text, string Page);
