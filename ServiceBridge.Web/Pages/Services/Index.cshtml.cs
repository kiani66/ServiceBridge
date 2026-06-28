using Microsoft.AspNetCore.Mvc.RazorPages;
using ServiceBridge.Application.DTOs;
using ServiceBridge.Application.Results;
using ServiceBridge.Shared.Constants;

namespace ServiceBridge.Web.Pages.Services;

public class IndexModel : PageModel
{
    private readonly IHttpClientFactory _httpClientFactory;

    public IndexModel(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public IReadOnlyCollection<ServiceSummaryResponse> Services { get; private set; } =
        Array.Empty<ServiceSummaryResponse>();

    public string? ErrorMessage { get; private set; }

    public async Task OnGetAsync()
    {
        var client = _httpClientFactory.CreateClient(AppConstants.Api.HttpClientName);

        try
        {
            var result = await client
                .GetFromJsonAsync<ServiceResult<IReadOnlyCollection<ServiceSummaryResponse>>>(
                    "api/services");

            if (result?.Success == true && result.Data != null)
            {
                Services = result.Data;
                return;
            }

            ErrorMessage = result?.Message ?? "Unable to load services.";
        }
        catch (HttpRequestException)
        {
            ErrorMessage = "Unable to connect to the server. Please try again later.";
        }
        catch (NotSupportedException)
        {
            ErrorMessage = "The server returned an unsupported response.";
        }
        catch (System.Text.Json.JsonException)
        {
            ErrorMessage = "The server returned an invalid response.";
        }
    }
}
