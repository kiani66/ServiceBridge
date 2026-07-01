using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ServiceBridge.Web.Navigation;

namespace ServiceBridge.Web.Pages;

public class IndexModel : PageModel
{
    public IActionResult OnGet()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            var landingPage = RoleNavigation.GetLandingPage(User);
            return landingPage == null
                ? Forbid()
                : IsBlazorRoute(landingPage)
                    ? LocalRedirect(landingPage)
                    : RedirectToPage(landingPage);
        }

        return RedirectToPage("/Account/Login");
    }

    private static bool IsBlazorRoute(string route)
    {
        return string.Equals(route, "/ui", StringComparison.OrdinalIgnoreCase)
            || route.StartsWith("/ui/", StringComparison.OrdinalIgnoreCase);
    }
}
