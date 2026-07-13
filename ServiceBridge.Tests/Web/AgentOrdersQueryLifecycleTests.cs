using ServiceBridge.Domain.Enums;
using ServiceBridge.Web.Services;
using Xunit;

namespace ServiceBridge.Tests.Web;

public class AgentOrdersQueryLifecycleTests
{
    [Fact]
    public async Task InitialParameterApplication_LoadsExactlyOnce()
    {
        var lifecycle = new AgentOrdersQueryLifecycle();
        var requests = new List<AgentOrdersQueryState>();
        var query = AgentOrdersQueryState.Parse(null, null, null);

        Assert.True(await lifecycle.ApplyAsync(query, Record));
        Assert.False(await lifecycle.ApplyAsync(query, Record));

        Assert.Equal([AgentOrdersQueryState.Default], requests);

        Task Record(AgentOrdersQueryState request)
        {
            requests.Add(request);
            return Task.CompletedTask;
        }
    }

    [Theory]
    [MemberData(nameof(QueryChanges))]
    public async Task QueryChange_NavigatesAndLoadsExactlyOnceAfterParameterApplication(
        AgentOrdersQueryState requested)
    {
        var lifecycle = new AgentOrdersQueryLifecycle();
        var requests = new List<AgentOrdersQueryState>();
        await lifecycle.ApplyAsync(AgentOrdersQueryState.Default, Record);

        Assert.True(lifecycle.ShouldNavigate(requested));
        Assert.True(await lifecycle.ApplyAsync(requested, Record));
        Assert.False(await lifecycle.ApplyAsync(requested, Record));
        Assert.False(lifecycle.ShouldNavigate(requested));

        Assert.Equal(2, requests.Count);
        Assert.Equal(requested, requests[1]);

        Task Record(AgentOrdersQueryState request)
        {
            requests.Add(request);
            return Task.CompletedTask;
        }
    }

    [Fact]
    public async Task Paging_PreservesActiveStatusAndSearch()
    {
        var lifecycle = new AgentOrdersQueryLifecycle();
        var active = new AgentOrdersQueryState(OrderStatus.Processing, "customer", 1);
        await lifecycle.ApplyAsync(active, _ => Task.CompletedTask);

        var requested = lifecycle.Current with { Page = 2 };

        Assert.Equal(OrderStatus.Processing, requested.Status);
        Assert.Equal("customer", requested.Search);
        Assert.Equal(2, requested.Page);
        Assert.True(lifecycle.ShouldNavigate(requested));
    }

    [Theory]
    [InlineData("Paid", OrderStatus.Paid)]
    [InlineData("paid", OrderStatus.Paid)]
    public void Parse_ValidStatus_IsCaseInsensitive(string value, OrderStatus expected)
    {
        Assert.Equal(expected, AgentOrdersQueryState.Parse(value, null, null).Status);
    }

    [Theory]
    [InlineData("invalid")]
    [InlineData("999")]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Parse_InvalidOrMissingStatus_RemovesFilter(string? value)
    {
        Assert.Null(AgentOrdersQueryState.Parse(value, null, null).Status);
    }

    [Theory]
    [InlineData("2", 2)]
    [InlineData("abc", 1)]
    [InlineData("0", 1)]
    [InlineData("-1", 1)]
    [InlineData("999999999999999999999999999999999999", 1)]
    [InlineData("", 1)]
    [InlineData(null, 1)]
    public void Parse_PageIsSafeAndPositive(string? value, int expected)
    {
        Assert.Equal(expected, AgentOrdersQueryState.Parse(null, null, value).Page);
    }

    public static IEnumerable<object[]> QueryChanges()
    {
        yield return [new AgentOrdersQueryState(OrderStatus.Paid, null, 1)];
        yield return [new AgentOrdersQueryState(null, "order-42", 1)];
        yield return [new AgentOrdersQueryState(null, null, 2)];
    }
}
