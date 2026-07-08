using System.Security.Claims;
using MudBlazor;
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
                new("Support", "/ui/admin/tickets"),
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
                new("Support", "/ui/tickets"),
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

    public static IReadOnlyList<NavigationItem> GetConsoleItems(ClaimsPrincipal user)
    {
        if (user.Identity?.IsAuthenticated != true)
        {
            return Array.Empty<NavigationItem>();
        }

        if (user.IsInRole(AppConstants.Roles.Admin))
        {
            return
            [
                new("Dashboard", "/ui/admin", "Admin operations", Icons.Material.Outlined.SpaceDashboard, MatchExact: true),
                new("Financial Reports", "/ui/admin/reports/financial", "Admin operations", Icons.Material.Outlined.Assessment),
                new("Audit", "/ui/admin/audit", "Admin operations", Icons.Material.Outlined.ManageSearch),
                new("Catalog", "/Admin/Catalog", "Admin operations", Icons.Material.Outlined.Inventory2),
                new("Pricing", "/Admin/Pricing", "Admin operations", Icons.Material.Outlined.PriceChange),
                new("Users", "/Admin/Users", "Admin operations", Icons.Material.Outlined.Group),
                new("Orders", "/Admin/Orders", "Admin operations", Icons.Material.Outlined.ReceiptLong),
                new("Notifications", "/ui/notifications", "Account", Icons.Material.Outlined.Notifications),
                new("Profile", "/ui/profile", "Account", Icons.Material.Outlined.Person)
            ];
        }

        if (user.IsInRole(AppConstants.Roles.Agent))
        {
            return
            [
                new("Dashboard", "/ui/agent", "Agent operations", Icons.Material.Outlined.SpaceDashboard, MatchExact: true),
                new("Assigned Orders", "/ui/agent/orders?status=Assigned", "Agent operations", Icons.Material.Outlined.AssignmentInd),
                new("Processing", "/ui/agent/orders?status=Processing", "Agent operations", Icons.Material.Outlined.ManageHistory),
                new("Completed", "/ui/agent/orders?status=Completed", "Agent operations", Icons.Material.Outlined.CheckCircle),
                new("Notifications", "/ui/notifications", "Account", Icons.Material.Outlined.Notifications),
                new("Profile", "/ui/profile", "Account", Icons.Material.Outlined.Person)
            ];
        }

        if (user.IsInRole(AppConstants.Roles.User))
        {
            return
            [
                new("Services", "/ui/catalog", "Customer", Icons.Material.Outlined.Apps),
                new("Orders", "/ui/orders", "Customer", Icons.Material.Outlined.ReceiptLong),
                new("Support", "/ui/tickets", "Customer", Icons.Material.Outlined.ContactSupport),
                new("Notifications", "/ui/notifications", "Account", Icons.Material.Outlined.Notifications),
                new("Profile", "/ui/profile", "Account", Icons.Material.Outlined.Person)
            ];
        }

        return Array.Empty<NavigationItem>();
    }

    public static string? GetConsoleLandingPage(ClaimsPrincipal user)
    {
        if (user.IsInRole(AppConstants.Roles.Admin))
        {
            return "/ui/admin";
        }

        if (user.IsInRole(AppConstants.Roles.Agent))
        {
            return "/ui/agent/orders";
        }

        if (user.IsInRole(AppConstants.Roles.User))
        {
            return "/ui/orders";
        }

        return null;
    }

    public static string? GetConsoleLandingPage(string role)
    {
        return role switch
        {
            AppConstants.Roles.Admin => "/ui/admin",
            AppConstants.Roles.Agent => "/ui/agent/orders",
            AppConstants.Roles.User => "/ui/orders",
            _ => null
        };
    }
}

public sealed record NavigationItem(
    string Text,
    string Page,
    string Group = "",
    string Icon = "",
    bool MatchExact = false);
