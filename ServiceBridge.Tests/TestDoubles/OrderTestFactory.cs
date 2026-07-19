using System.Reflection;
using ServiceBridge.Domain.Entities;
using ServiceBridge.Domain.Enums;

namespace ServiceBridge.Tests.TestDoubles;

internal static class OrderTestFactory
{
    private static readonly ConstructorInfo Constructor = typeof(Order)
        .GetConstructor(
            BindingFlags.Instance | BindingFlags.NonPublic,
            binder: null,
            Type.EmptyTypes,
            modifiers: null)!;

    private static readonly PropertyInfo RuntimeFulfillmentMode = typeof(Order)
        .GetProperty(nameof(Order.ServiceFulfillmentModeSnapshot))!;

    public static Order Create(
        FulfillmentMode runtimeFulfillmentMode = FulfillmentMode.ManualAgent)
    {
        var order = (Order)Constructor.Invoke(null);
        RuntimeFulfillmentMode.SetValue(order, runtimeFulfillmentMode);
        return order;
    }
}
