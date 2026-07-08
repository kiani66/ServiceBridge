using System.Net;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ServiceBridge.Application.DTOs;
using ServiceBridge.Application.Results;
using ServiceBridge.Shared.Constants;
using ServiceBridge.Shared.Messages;

namespace ServiceBridge.Web.Pages;

[Authorize(Roles = AppConstants.Roles.Admin)]
public class DashboardModel : PageModel
{
    private readonly IHttpClientFactory _httpClientFactory;

    public DashboardModel(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public AdminOrderDashboardSummaryDto Summary { get; private set; } = new();
    public string? ErrorMessage { get; private set; }

    public Task<IActionResult> OnGetAsync()
    {
        return Task.FromResult<IActionResult>(LocalRedirect("/ui/admin"));
    }
}
