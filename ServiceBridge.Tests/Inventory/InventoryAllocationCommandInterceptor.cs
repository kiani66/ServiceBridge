using System.Collections.Concurrent;
using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace ServiceBridge.Tests.Inventory;

public sealed record RecordedInventoryAllocationCommand(
    string CommandText,
    IReadOnlyList<object?> ParameterValues);

public sealed class InventoryAllocationCommandInterceptor : DbCommandInterceptor
{
    private readonly ConcurrentQueue<RecordedInventoryAllocationCommand> _commands = new();

    public IReadOnlyCollection<RecordedInventoryAllocationCommand> Commands =>
        _commands.ToArray();

    public override InterceptionResult<DbDataReader> ReaderExecuting(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<DbDataReader> result)
    {
        Record(command);
        return base.ReaderExecuting(command, eventData, result);
    }

    public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<DbDataReader> result,
        CancellationToken cancellationToken = default)
    {
        Record(command);
        return base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
    }

    private void Record(DbCommand command)
    {
        _commands.Enqueue(new RecordedInventoryAllocationCommand(
            command.CommandText,
            command.Parameters.Cast<DbParameter>()
                .Select(parameter => parameter.Value)
                .ToArray()));
    }
}
