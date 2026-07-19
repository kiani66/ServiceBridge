using Microsoft.EntityFrameworkCore;
using ServiceBridge.Application.Interfaces;
using ServiceBridge.Domain.Entities;
using ServiceBridge.Infrastructure.Data;

namespace ServiceBridge.Infrastructure.Repositories;

public sealed class FulfillmentExecutionRepository :
    IFulfillmentExecutionRepository
{
    private readonly ServiceBridgeDbContext _context;

    public FulfillmentExecutionRepository(ServiceBridgeDbContext context)
    {
        _context = context;
    }

    public Task<bool> ExistsForOrderAsync(
        Guid orderId,
        CancellationToken cancellationToken = default)
    {
        if (_context.FulfillmentExecutions.Local.Any(x => x.OrderId == orderId))
        {
            return Task.FromResult(true);
        }

        return _context.FulfillmentExecutions
            .AsNoTracking()
            .AnyAsync(x => x.OrderId == orderId, cancellationToken);
    }

    public Task<FulfillmentExecution?> GetForMutationAsync(
        Guid orderId,
        CancellationToken cancellationToken = default)
    {
        var local = _context.FulfillmentExecutions.Local
            .SingleOrDefault(x => x.OrderId == orderId);
        if (local is not null)
        {
            return Task.FromResult<FulfillmentExecution?>(local);
        }

        return _context.FulfillmentExecutions
            .Include(x => x.Attempts)
            .SingleOrDefaultAsync(x => x.OrderId == orderId, cancellationToken);
    }

    public async Task AddAsync(
        FulfillmentExecution execution,
        CancellationToken cancellationToken = default)
    {
        await _context.FulfillmentExecutions.AddAsync(
            execution,
            cancellationToken);
    }
}
