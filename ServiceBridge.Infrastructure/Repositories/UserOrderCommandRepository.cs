using Microsoft.EntityFrameworkCore;
using ServiceBridge.Application.Interfaces;
using ServiceBridge.Domain.Entities;
using ServiceBridge.Infrastructure.Data;

namespace ServiceBridge.Infrastructure.Repositories;

public class UserOrderCommandRepository : IUserOrderCommandRepository
{
    private readonly ServiceBridgeDbContext _context;

    public UserOrderCommandRepository(ServiceBridgeDbContext context)
    {
        _context = context;
    }

    public Task<Order?> GetOwnedOrderForUpdateAsync(Guid userId, Guid orderId)
    {
        return _context.Orders
            .Include(x => x.CustomerActionRequests)
            .FirstOrDefaultAsync(x => x.Id == orderId && x.UserId == userId);
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
