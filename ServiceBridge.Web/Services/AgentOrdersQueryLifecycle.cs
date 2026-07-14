using ServiceBridge.Domain.Enums;

namespace ServiceBridge.Web.Services;

public sealed record AgentOrdersQueryState(
    OrderStatus? Status,
    string? Search,
    int Page)
{
    public static AgentOrdersQueryState Default { get; } = new(null, null, 1);

    public static AgentOrdersQueryState Parse(
        string? status,
        string? search,
        string? page)
    {
        return new AgentOrdersQueryState(
            ParseStatus(status),
            NormalizeSearch(search),
            ParsePage(page));
    }

    private static OrderStatus? ParseStatus(string? status)
    {
        if (string.IsNullOrWhiteSpace(status))
        {
            return null;
        }

        return Enum.TryParse<OrderStatus>(status, ignoreCase: true, out var parsed) &&
               Enum.IsDefined(parsed)
            ? parsed
            : null;
    }

    private static int ParsePage(string? page)
    {
        return int.TryParse(page, out var parsed) && parsed > 0
            ? parsed
            : 1;
    }

    private static string? NormalizeSearch(string? search)
    {
        return string.IsNullOrWhiteSpace(search) ? null : search;
    }
}

public sealed class AgentOrdersQueryLifecycle
{
    private AgentOrdersQueryState? _current;

    public AgentOrdersQueryState Current => _current ?? AgentOrdersQueryState.Default;

    public bool ShouldNavigate(AgentOrdersQueryState requested)
    {
        ArgumentNullException.ThrowIfNull(requested);
        return _current != requested;
    }

    public async Task<bool> ApplyAsync(
        AgentOrdersQueryState query,
        Func<AgentOrdersQueryState, Task> loadAsync)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(loadAsync);

        if (_current == query)
        {
            return false;
        }

        _current = query;
        await loadAsync(query);
        return true;
    }
}
