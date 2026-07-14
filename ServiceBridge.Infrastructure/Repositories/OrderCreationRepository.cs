using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using ServiceBridge.Application.DTOs;
using ServiceBridge.Application.Interfaces;
using ServiceBridge.Domain.Entities;
using ServiceBridge.Infrastructure.Data;

namespace ServiceBridge.Infrastructure.Repositories;

public class OrderCreationRepository : IOrderCreationRepository
{
    private readonly ServiceBridgeDbContext _context;

    public OrderCreationRepository(ServiceBridgeDbContext context)
    {
        _context = context;
    }

    public Task<bool> UserExistsAsync(Guid userId)
    {
        return _context.Users.AnyAsync(x => x.Id == userId && x.IsActive);
    }

    public async Task<OrderServiceDefinition?> GetServiceVariantOrderDefinitionAsync(
        Guid serviceVariantId,
        Guid? serviceId)
    {
        return await _context.ServiceVariants
            .AsNoTracking()
            .Where(x =>
                x.Id == serviceVariantId &&
                (!serviceId.HasValue || x.ServiceId == serviceId.Value))
            .Select(x => new OrderServiceDefinition
            {
                ServiceId = x.ServiceId,
                CategoryId = x.Service.CategoryId,
                ServiceVariantId = x.Id,
                IsVariantActive = x.IsActive,
                IsServiceActive = x.Service.IsActive,
                IsCategoryActive = x.Service.Category.IsActive,
                IsProviderActive = x.Service.Provider.IsActive,
                VariantName = x.Name,
                VariantDurationDays = x.DurationDays,
                VariantRegionCode = x.RegionCode,
                CurrencyCode = x.CurrencyCode,
                BasePrice = x.BasePrice,
                ServiceFulfillmentMode = x.FulfillmentMode,
                SupportsUseExistingAccount = x.Service.SupportsUseExistingAccount,
                SupportsCreateNewAccount = x.Service.SupportsCreateNewAccount,
                RequiredFields = x.Service.RequiredFields
                    .OrderBy(field => field.DisplayOrder)
                    .Select(field => new OrderFieldDefinition
                    {
                        FieldKey = field.FieldKey,
                        Label = field.Label,
                        FieldType = field.FieldType.ToString(),
                        FulfillmentMode = field.FulfillmentMode,
                        Actor = field.Actor,
                        IsRequired = field.IsRequired,
                        IsSensitive = field.IsSensitive,
                        IsVisibleToCustomerAfterCompletion =
                            field.IsVisibleToCustomerAfterCompletion,
                        DisplayOrder = field.DisplayOrder
                    })
                    .ToList()
            })
            .FirstOrDefaultAsync();
    }

    public Task<bool> OrderNumberExistsAsync(string orderNumber)
    {
        return _context.Orders.AnyAsync(x => x.OrderNumber == orderNumber);
    }

    public async Task AddOrderAsync(Order order)
    {
        await _context.Orders.AddAsync(order);
    }

    public async Task<bool> SaveChangesAsync()
    {
        try
        {
            await _context.SaveChangesAsync();
            return true;
        }
        catch (DbUpdateException exception)
            when (exception.InnerException is SqlException { Number: 2601 or 2627 })
        {
            return false;
        }
    }
}
