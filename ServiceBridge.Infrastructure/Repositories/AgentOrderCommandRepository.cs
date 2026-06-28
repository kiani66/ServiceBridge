using Microsoft.EntityFrameworkCore;
using ServiceBridge.Application.Interfaces;
using ServiceBridge.Domain.Entities;
using ServiceBridge.Infrastructure.Data;

namespace ServiceBridge.Infrastructure.Repositories;

public class AgentOrderCommandRepository : IAgentOrderCommandRepository
{
    private readonly ServiceBridgeDbContext _context;

    public AgentOrderCommandRepository(ServiceBridgeDbContext context)
    {
        _context = context;
    }

    public Task<Order?> GetAssignedOrderForUpdateAsync(
        Guid agentId,
        Guid orderId)
    {
        return _context.Orders
            .Include(x => x.CustomerActionRequests)
            .Include(x => x.FieldValues)
            .Include(x => x.Service)
                .ThenInclude(x => x.RequiredFields)
            .FirstOrDefaultAsync(x =>
                x.Id == orderId &&
                x.AssignedAgentId == agentId);
    }

    public async Task<bool> SaveChangesAsync()
    {
        try
        {
            await _context.SaveChangesAsync();
            return true;
        }
        catch (DbUpdateConcurrencyException)
        {
            return false;
        }
    }
}
