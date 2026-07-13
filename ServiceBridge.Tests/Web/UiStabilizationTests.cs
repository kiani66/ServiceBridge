using System.Reflection;
using ServiceBridge.Application.DTOs;
using ServiceBridge.Web.Components.Pages;
using Xunit;

namespace ServiceBridge.Tests.Web;

public class UiStabilizationTests
{
    [Fact]
    public void CloseCreateAgent_ClearsSensitiveRequestAndDrawerState()
    {
        var component = new AdminUsers();
        var requestField = PrivateField("_agentRequest");
        var originalRequest = new CreateAgentRequest
        {
            FullName = "Sensitive User",
            Email = "sensitive@example.test",
            Password = "NeverRetainThis123!"
        };

        requestField.SetValue(component, originalRequest);
        PrivateField("_createAgentOpen").SetValue(component, true);
        PrivateField("_actionError").SetValue(component, "Previous error");

        typeof(AdminUsers)
            .GetMethod("CloseCreateAgent", BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(component, null);

        var clearedRequest = Assert.IsType<CreateAgentRequest>(requestField.GetValue(component));
        Assert.NotSame(originalRequest, clearedRequest);
        Assert.Empty(clearedRequest.FullName);
        Assert.Empty(clearedRequest.Email);
        Assert.Empty(clearedRequest.Password);
        Assert.False((bool)PrivateField("_createAgentOpen").GetValue(component)!);
        Assert.Null(PrivateField("_actionError").GetValue(component));
    }

    [Fact]
    public void AppShell_DoesNotOwnFinalRealtimeDisposal()
    {
        var source = File.ReadAllText(ComponentPath("Layout", "AppShell.razor"));

        Assert.Contains("await RealtimeClient.StartAsync();", source);
        Assert.DoesNotContain("RealtimeClient.DisposeAsync", source);
    }

    [Fact]
    public void ClosePasswordReset_ClearsSensitiveStateAndDrawerError()
    {
        var component = new AdminUsers();
        PrivateField("_passwordResetOpen").SetValue(component, true);
        PrivateField("_newPassword").SetValue(component, "NeverRetainThis456!");
        PrivateField("_passwordResetUser").SetValue(component, new AdminUserListItemDto());
        PrivateField("_passwordResetError").SetValue(component, "Previous error");

        typeof(AdminUsers)
            .GetMethod("ClosePasswordReset", BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(component, null);

        Assert.False((bool)PrivateField("_passwordResetOpen").GetValue(component)!);
        Assert.Null(PrivateField("_newPassword").GetValue(component));
        Assert.Null(PrivateField("_passwordResetUser").GetValue(component));
        Assert.Null(PrivateField("_passwordResetError").GetValue(component));
    }

    [Fact]
    public void Logout_StopsRealtimeBeforeSubmittingExistingPostForm()
    {
        var source = File.ReadAllText(ComponentPath("Navigation", "AccountMenu.razor"));
        var stopIndex = source.IndexOf("await RealtimeClient.StopAsync();", StringComparison.Ordinal);
        var submitIndex = source.IndexOf("serviceBridgeSubmitForm", StringComparison.Ordinal);

        Assert.True(stopIndex >= 0);
        Assert.True(submitIndex > stopIndex);
        Assert.Contains("<form @ref=\"_logoutForm\" method=\"post\" action=\"/Account/Logout\" hidden>", source);
        Assert.Contains("<AntiforgeryToken />", source);
        Assert.DoesNotContain("<form @ref=\"_logoutForm\"", source[..source.IndexOf("</MudMenu>", StringComparison.Ordinal)]);
    }

    [Theory]
    [InlineData("AdminCatalog.razor")]
    [InlineData("AdminCatalogService.razor")]
    [InlineData("AdminPricing.razor")]
    [InlineData("AdminUsers.razor")]
    public void MutationRunners_GuardReentryAndResetBusyInFinally(string pageName)
    {
        var source = File.ReadAllText(ComponentPath("Pages", pageName));

        Assert.Contains("if (_actionBusy)", source);
        Assert.Contains("_actionBusy = true;", source);
        Assert.Contains("finally", source);
        Assert.Contains("_actionBusy = false;", source);
    }

    [Fact]
    public void CatalogService_DrawerErrorsAreLocalAndCallerOwned()
    {
        var source = File.ReadAllText(ComponentPath("Pages", "AdminCatalogService.razor"));

        Assert.Contains("_variantActionError", source);
        Assert.Contains("_fieldActionError", source);
        Assert.Contains("OnCloseRequested=\"CloseVariantDrawer\"", source);
        Assert.Contains("OnCloseRequested=\"CloseFieldDrawer\"", source);
    }

    [Fact]
    public void AppShell_SeparatesMobileDrawerAndClosesOnNavigation()
    {
        var source = File.ReadAllText(ComponentPath("Layout", "AppShell.razor"));

        Assert.Contains("private bool _mobileDrawerOpen;", source);
        Assert.Contains("private bool _desktopDrawerOpen = true;", source);
        Assert.Contains("DrawerVariant.Temporary", source);
        Assert.Contains("class=\"sb-desktop-sidebar\"", source);
        Assert.DoesNotContain("DrawerVariant.Persistent", source);
        Assert.DoesNotContain("MudMainContent", source);
        Assert.Contains("Navigation.LocationChanged += HandleLocationChanged;", source);
        Assert.Contains("OnNavigate=\"HandleNavigationSelectedAsync\"", source);
    }

    [Theory]
    [InlineData(typeof(Home))]
    [InlineData(typeof(Catalog))]
    [InlineData(typeof(Category))]
    [InlineData(typeof(ServiceDetails))]
    public void PublicRoutes_AlwaysUsePublicLayout(Type pageType)
    {
        var layout = pageType.GetCustomAttribute<Microsoft.AspNetCore.Components.LayoutAttribute>();

        Assert.NotNull(layout);
        Assert.Equal(typeof(ServiceBridge.Web.Components.Layout.PublicLayout), layout.LayoutType);
        Assert.False(File.Exists(ComponentPath("Layout", "PublicOrDashboardLayout.razor")));
    }

    [Fact]
    public void PublicChrome_UsesExclusiveAuthenticationActionsAndSharedAccountMenu()
    {
        var source = File.ReadAllText(ComponentPath("Layout", "PublicChrome.razor"));

        Assert.Contains("<Authorized Context=\"publicAuthContext\">", source);
        Assert.Contains("<NotAuthorized>", source);
        Assert.Contains("<AccountMenu User=\"@publicAuthContext.User\" />", source);
        Assert.Contains("Open workspace", source);
        Assert.Contains("Sign in", source);
    }

    [Theory]
    [InlineData("Navigation", "AccountMenu.razor")]
    [InlineData("Navigation", "NotificationBell.razor")]
    [InlineData("Layout", "PublicChrome.razor")]
    public void CustomMudMenuActivators_ExplicitlyToggleTheirMenuContext(string folder, string fileName)
    {
        var source = File.ReadAllText(ComponentPath(folder, fileName));

        Assert.Contains("OnClick=\"@menuContext.ToggleAsync\"", source);
    }

    [Fact]
    public void NotificationDropdown_UsesKeyboardNativeItemsAndPreservesMenuActions()
    {
        var source = File.ReadAllText(ComponentPath("Navigation", "NotificationBell.razor"));

        Assert.Contains("<button class=\"sb-notification-item", source);
        Assert.Contains("type=\"button\"", source);
        Assert.Contains("@onclick=\"() => OpenNotificationAsync(notification)\"", source);
        Assert.Contains("OnClick=\"MarkAllAsReadAsync\"", source);
        Assert.Contains("Href=\"/ui/notifications\"", source);
        Assert.Contains("CloseMenuAsync", source);
        Assert.Contains("_openingNotifications.Add(notification.Id)", source);
    }

    [Fact]
    public void NotificationDropdown_CoalescesRealtimeRefreshesAndDeduplicatesItems()
    {
        var source = File.ReadAllText(ComponentPath("Navigation", "NotificationBell.razor"));

        Assert.Contains("_refreshTask is { IsCompleted: false }", source);
        Assert.Contains("DistinctBy(notification => notification.Id)", source);
        Assert.Equal(
            1,
            source.Split("SubscribeToNotifications", StringSplitOptions.None).Length - 1);
    }

    [Fact]
    public void NotificationListProjection_ContainsOnlyUiFieldsAndNoEntityInclude()
    {
        var properties = typeof(NotificationDto)
            .GetProperties()
            .Select(property => property.Name)
            .OrderBy(name => name)
            .ToArray();
        var repository = File.ReadAllText(InfrastructurePath(
            "Repositories",
            "NotificationRepository.cs"));

        Assert.Equal(
            new[] { "Body", "CreatedAtUtc", "Id", "IsRead", "LinkUrl", "Title" },
            properties);
        Assert.DoesNotContain(".Include(", repository);
        Assert.Contains("LinkUrl = x.LinkUrl", repository);
    }

    [Fact]
    public void AuthenticatedShell_UsesGridOwnedDesktopSidebarAndTemporaryMobileDrawer()
    {
        var source = File.ReadAllText(ComponentPath("Layout", "AppShell.razor"));
        var css = File.ReadAllText(WebPath("wwwroot", "css", "design-system.css"));

        Assert.Contains("grid-template-columns: var(--sb-sidebar-width) minmax(0, 1fr);", css);
        Assert.Contains(".sb-shell-workspace", css);
        Assert.Contains(".sb-desktop-sidebar", css);
        Assert.Contains(".sb-mobile-sidebar", css);
        Assert.DoesNotContain("padding: calc(var(--sb-topbar-height)", css);
        Assert.DoesNotContain("width: 100vw", css);
        Assert.DoesNotContain("@bind-Open", source);
    }

    [Fact]
    public void UserDashboard_RetainsTheUiUserCompatibilityRoute()
    {
        var source = File.ReadAllText(ComponentPath("Pages", "Dashboard.razor"));

        Assert.Contains("@page \"/ui/dashboard\"", source);
        Assert.Contains("@page \"/ui/user\"", source);
        Assert.Contains("@attribute [Authorize(Roles = AppConstants.Roles.User)]", source);
    }

    [Fact]
    public void SharedDrawer_DefinesDialogSemanticsFocusAndBusyCloseGuard()
    {
        var source = File.ReadAllText(ComponentPath("Shared", "Drawer.razor"));

        Assert.Contains("role=\"dialog\"", source);
        Assert.Contains("aria-labelledby=\"@_titleId\"", source);
        Assert.Contains("<MudFocusTrap", source);
        Assert.Contains("if (Busy)", source);
        Assert.Contains("serviceBridgeFocusById", source);
    }

    [Fact]
    public void Pricing_UsesQueryPersistedTabsWithContextualActions()
    {
        var source = File.ReadAllText(ComponentPath("Pages", "AdminPricing.razor"));

        Assert.Contains("[SupplyParameterFromQuery(Name = \"section\")]", source);
        Assert.Contains("<MudTabs", source);
        Assert.Contains("<MudTabPanel Text=\"Exchange Rates\">", source);
        Assert.Contains("<MudTabPanel Text=\"Profit Margins\">", source);
        Assert.Contains("OnActiveTabChanged", source);
        Assert.Contains("_activeTabIndex == 0", source);
    }

    [Fact]
    public void Css_HasNoGlobalMinimumWidthOrDeletedRazorShellSelectors()
    {
        var designSystem = File.ReadAllText(WebPath("wwwroot", "css", "design-system.css"));
        var site = File.ReadAllText(WebPath("wwwroot", "css", "site.css"));

        Assert.DoesNotContain("min-width: 320px", designSystem);
        Assert.DoesNotContain(".sb-razor-", site);
    }

    private static FieldInfo PrivateField(string name)
    {
        return typeof(AdminUsers).GetField(
            name,
            BindingFlags.Instance | BindingFlags.NonPublic)!;
    }

    private static string ComponentPath(string folder, string fileName)
    {
        return WebPath(
            "Components",
            folder,
            fileName);
    }

    private static string WebPath(params string[] parts)
    {
        var root = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..",
            "..",
            "..",
            ".."));
        return Path.Combine([root, "ServiceBridge.Web", .. parts]);
    }

    private static string InfrastructurePath(params string[] parts)
    {
        var root = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..",
            "..",
            "..",
            ".."));
        return Path.Combine([root, "ServiceBridge.Infrastructure", .. parts]);
    }
}
