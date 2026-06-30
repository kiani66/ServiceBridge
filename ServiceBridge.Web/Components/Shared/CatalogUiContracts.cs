namespace ServiceBridge.Web.Components.Shared;

public interface IServiceCardModel
{
    string Slug { get; }
    string Name { get; }
    string ProviderName { get; }
    string ProviderShortName { get; }
    string CategoryName { get; }
    string Summary { get; }
    string DetailHref { get; }
    string PriceLabel { get; }
    string ProcessingTime { get; }
}

public interface IServiceVariantOption
{
    string Id { get; }
    string Name { get; }
    string Description { get; }
    string PriceText { get; }
    string MetaText { get; }
    bool IsPopular { get; }
}
