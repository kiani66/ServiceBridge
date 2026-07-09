using System.Net;
using System.Net.Http.Json;
using ServiceBridge.Application.DTOs;
using ServiceBridge.Application.Results;
using ServiceBridge.Domain.Enums;
using ServiceBridge.Shared.Constants;
using ServiceBridge.Web.Services;
using Xunit;

namespace ServiceBridge.Tests.Web;

public class AdminAuditLogsClientTests
{
    [Fact]
    public async Task GetAuditLogsAsync_PassesPagedFilters()
    {
        HttpRequestMessage? capturedRequest = null;
        var handler = new StubHttpMessageHandler(request =>
        {
            capturedRequest = request;
            return Task.FromResult(JsonResponse(ServiceResult<PagedResultDto<AuditLogListItemDto>>.Ok(
                new PagedResultDto<AuditLogListItemDto>
                {
                    Items =
                    [
                        new AuditLogListItemDto
                        {
                            Id = Guid.NewGuid(),
                            ActorRole = "Admin",
                            ActionType = nameof(AuditActionType.OrderAssigned),
                            EntityName = "Order"
                        }
                    ],
                    Page = 2,
                    PageSize = 25,
                    TotalCount = 1
                })));
        });
        var client = new AdminAuditLogsClient(new StubHttpClientFactory(handler));
        var actorId = Guid.NewGuid();
        var entityId = Guid.NewGuid();

        var result = await client.GetAuditLogsAsync(
            2,
            25,
            actorId,
            "Order",
            entityId,
            AuditActionType.OrderAssigned);

        Assert.True(result.Success);
        Assert.NotNull(capturedRequest);
        Assert.Equal("/api/admin/audit-logs", capturedRequest!.RequestUri!.AbsolutePath);

        var query = ParseQuery(capturedRequest.RequestUri.Query);
        Assert.Equal("2", query["page"]);
        Assert.Equal("25", query["pageSize"]);
        Assert.Equal(actorId.ToString(), query["actorId"]);
        Assert.Equal("Order", query["entityName"]);
        Assert.Equal(entityId.ToString(), query["entityId"]);
        Assert.Equal(nameof(AuditActionType.OrderAssigned), query["actionType"]);
    }

    [Fact]
    public void AuditLogListItemDto_DoesNotExposeStoredJsonPayloads()
    {
        var properties = typeof(AuditLogListItemDto)
            .GetProperties()
            .Select(x => x.Name)
            .ToArray();

        Assert.DoesNotContain("BeforeJson", properties);
        Assert.DoesNotContain("AfterJson", properties);
    }

    private static HttpResponseMessage JsonResponse<T>(T value)
    {
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(value)
        };

        return response;
    }

    private static Dictionary<string, string> ParseQuery(string query)
    {
        return query
            .TrimStart('?')
            .Split('&', StringSplitOptions.RemoveEmptyEntries)
            .Select(part => part.Split('=', 2))
            .ToDictionary(
                parts => Uri.UnescapeDataString(parts[0]),
                parts => parts.Length == 2
                    ? Uri.UnescapeDataString(parts[1])
                    : string.Empty);
    }

    private sealed class StubHttpClientFactory(HttpMessageHandler handler)
        : IHttpClientFactory
    {
        public HttpClient CreateClient(string name)
        {
            Assert.Equal(AppConstants.Api.HttpClientName, name);
            return new HttpClient(handler)
            {
                BaseAddress = new Uri("https://api.test/")
            };
        }
    }

    private sealed class StubHttpMessageHandler(
        Func<HttpRequestMessage, Task<HttpResponseMessage>> responseFactory)
        : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            return responseFactory(request);
        }
    }
}
