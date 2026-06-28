using Microsoft.EntityFrameworkCore;
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

    public async Task<OrderServiceDefinition?> GetActiveServiceDefinitionAsync(
        Guid serviceId,
        Guid? planId)
    {
        var service = await _context.DigitalServices
            .AsNoTracking()
            .Where(x =>
                x.Id == serviceId &&
                x.IsActive &&
                x.Category.IsActive)
            .Select(x => new OrderServiceDefinition
            {
                ServiceId = x.Id,
                CategoryId = x.CategoryId,
                HasActiveVariants = x.Variants.Any(plan => plan.IsActive),
                CurrencyCode = x.CurrencyCode,
                BasePrice = x.BasePrice,
                SupportsUseExistingAccount = x.SupportsUseExistingAccount,
                SupportsCreateNewAccount = x.SupportsCreateNewAccount,
                RequiredFields = x.RequiredFields
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

        if (service == null || !planId.HasValue)
        {
            return service;
        }

        var plan = await _context.ServiceVariants
            .AsNoTracking()
            .Where(x =>
                x.Id == planId.Value &&
                x.ServiceId == serviceId &&
                x.IsActive)
            .Select(x => new
            {
                x.Id,
                x.Name,
                x.DurationDays,
                x.RegionCode,
                x.CurrencyCode,
                x.BasePrice
            })
            .FirstOrDefaultAsync();

        if (plan == null)
        {
            return null;
        }

        service.VariantId = plan.Id;
        service.VariantName = plan.Name;
        service.VariantDurationDays = plan.DurationDays;
        service.VariantRegionCode = plan.RegionCode;
        service.CurrencyCode = plan.CurrencyCode;
        service.BasePrice = plan.BasePrice;
        return service;
    }

    public Task<bool> OrderNumberExistsAsync(string orderNumber)
    {
        return _context.Orders.AnyAsync(x => x.OrderNumber == orderNumber);
    }

    public async Task AddOrderAsync(Order order)
    {
        await _context.Orders.AddAsync(order);
        await _context.SaveChangesAsync();
    }
}
