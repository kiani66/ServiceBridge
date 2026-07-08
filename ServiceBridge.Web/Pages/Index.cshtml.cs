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
            var landingPage = RoleNavigation.GetConsoleLandingPage(User);
            return landingPage == null
                ? Forbid()
                : LocalRedirect(landingPage);
        }

        return LocalRedirect("/ui");
    }
}
