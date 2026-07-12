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
                new("Catalog", "/ui/admin/catalog"),
                new("Inventory", "/ui/admin/inventory"),
                new("Pricing", "/ui/admin/pricing"),
                new("Users", "/ui/admin/users"),
                new("Orders", "/ui/admin/orders"),
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
                new("Dashboard", "/ui/admin", "Overview", Icons.Material.Outlined.SpaceDashboard, MatchExact: true),
                new("Orders", "/ui/admin/orders", "Operations", Icons.Material.Outlined.ReceiptLong),
                new("Users", "/ui/admin/users", "Operations", Icons.Material.Outlined.Group),
                new("Support", "/ui/admin/tickets", "Operations", Icons.Material.Outlined.ContactSupport),
                new("Refunds", "/ui/admin/refunds", "Operations", Icons.Material.Outlined.AssignmentReturn),
                new("Catalog", "/ui/admin/catalog", "Commerce", Icons.Material.Outlined.Inventory2),
                new("Inventory", "/ui/admin/inventory", "Commerce", Icons.Material.Outlined.Inventory),
                new("Pricing", "/ui/admin/pricing", "Commerce", Icons.Material.Outlined.PriceChange),
                new("Financial Reports", "/ui/admin/reports/financial", "Insights", Icons.Material.Outlined.Assessment),
                new("Order Audit", "/ui/admin/audit", "Insights", Icons.Material.Outlined.ManageSearch),
                new("Audit Logs", "/ui/admin/audit-logs", "Insights", Icons.Material.Outlined.FactCheck),
                new("Notifications", "/ui/notifications", "Account", Icons.Material.Outlined.Notifications),
                new("Profile", "/ui/profile", "Account", Icons.Material.Outlined.Person)
            ];
        }

        if (user.IsInRole(AppConstants.Roles.Agent))
        {
            return
            [
                new("Dashboard", "/ui/agent", "Overview", Icons.Material.Outlined.SpaceDashboard, MatchExact: true),
                new("Orders", "/ui/agent/orders", "Operations", Icons.Material.Outlined.AssignmentInd),
                new("Notifications", "/ui/notifications", "Account", Icons.Material.Outlined.Notifications),
                new("Profile", "/ui/profile", "Account", Icons.Material.Outlined.Person)
            ];
        }

        if (user.IsInRole(AppConstants.Roles.User))
        {
            return
            [
                new("Dashboard", "/ui/dashboard", "Workspace", Icons.Material.Outlined.SpaceDashboard, MatchExact: true),
                new("Services", "/ui/catalog", "Workspace", Icons.Material.Outlined.Apps),
                new("Orders", "/ui/orders", "Workspace", Icons.Material.Outlined.ReceiptLong),
                new("Support", "/ui/tickets", "Workspace", Icons.Material.Outlined.ContactSupport),
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
            return "/ui/dashboard";
        }

        return null;
    }

    public static string? GetConsoleLandingPage(string role)
    {
        return role switch
        {
            AppConstants.Roles.Admin => "/ui/admin",
            AppConstants.Roles.Agent => "/ui/agent/orders",
            AppConstants.Roles.User => "/ui/dashboard",
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
