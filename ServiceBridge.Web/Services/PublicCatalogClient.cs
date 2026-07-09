using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using ServiceBridge.Application.DTOs;
using ServiceBridge.Application.Results;
using ServiceBridge.Domain.Enums;
using ServiceBridge.Shared.Constants;
using ServiceBridge.Web.Components.Shared;

namespace ServiceBridge.Web.Services;

public sealed class PublicCatalogClient
{
    private static readonly Regex SlugUnsafeCharacters = new("[^a-z0-9]+", RegexOptions.Compiled);
    private readonly IHttpClientFactory _httpClientFactory;

    public PublicCatalogClient(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<PublicCatalogResult> GetCatalogAsync(
        CancellationToken cancellationToken = default)
    {
        var servicesResult = await GetActiveServicesAsync(loadQuotes: true, cancellationToken);
        if (!servicesResult.Success)
        {
            return PublicCatalogResult.Fail(servicesResult.ErrorMessage);
        }

        var services = servicesResult.Data;
        var categories = services
            .GroupBy(service => new { service.CategoryId, service.CategoryName })
            .Select(group => new PublicCatalogCategory(
                group.Key.CategoryId,
                group.Key.CategoryName,
                Slugify(group.Key.CategoryName),
                $"{group.Count()} active service{(group.Count() == 1 ? string.Empty : "s")} available"))
            .OrderBy(category => category.Name)
            .ToArray();

        return PublicCatalogResult.Ok(categories, services);
    }

    public async Task<PublicServiceDetailResult> GetServiceDetailAsync(
        string? routeKey,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(routeKey))
        {
            return PublicServiceDetailResult.Fail("The requested service is not available.");
        }

        Guid serviceId;
        if (!Guid.TryParse(routeKey, out serviceId))
        {
            return await GetServiceDetailFromEndpointAsync(
                $"api/services/slug/{Uri.EscapeDataString(routeKey.Trim())}",
                cancellationToken);
        }

        return await GetServiceDetailFromEndpointAsync(
            $"api/services/{serviceId}",
            cancellationToken);
    }

    private async Task<PublicServiceDetailResult> GetServiceDetailFromEndpointAsync(
        string endpoint,
        CancellationToken cancellationToken)
    {
        var client = _httpClientFactory.CreateClient(AppConstants.Api.HttpClientName);
        try
        {
            var result = await client
                .GetFromJsonAsync<ServiceResult<ServiceDetailResponse>>(
                    endpoint,
                    cancellationToken);

            if (result?.Success != true || result.Data is null)
            {
                return result?.StatusCode == 404
                    ? PublicServiceDetailResult.Missing()
                    : PublicServiceDetailResult.Fail(
                        result?.Message ?? "Unable to load the requested service.");
            }

            return PublicServiceDetailResult.Ok(MapDetail(result.Data));
        }
        catch (HttpRequestException)
        {
            return PublicServiceDetailResult.Fail("The service catalog API is unavailable.");
        }
        catch (System.Text.Json.JsonException)
        {
            return PublicServiceDetailResult.Fail("The service catalog response could not be read.");
        }
    }

    public async Task<PublicQuoteResult> GetQuoteAsync(
        Guid serviceId,
        Guid? serviceVariantId,
        CancellationToken cancellationToken = default)
    {
        var quote = await GetQuoteOrNullAsync(serviceId, serviceVariantId, cancellationToken);
        return quote is null
            ? PublicQuoteResult.Fail("Live pricing is not available for this selection.")
            : PublicQuoteResult.Ok(quote);
    }

    private async Task<ActiveServicesResult> GetActiveServicesAsync(
        bool loadQuotes,
        CancellationToken cancellationToken)
    {
        var client = _httpClientFactory.CreateClient(AppConstants.Api.HttpClientName);
        try
        {
            var result = await client
                .GetFromJsonAsync<ServiceResult<IReadOnlyCollection<ServiceSummaryResponse>>>(
                    "api/services",
                    cancellationToken);

            if (result?.Success != true || result.Data is null)
            {
                return ActiveServicesResult.Fail(
                    result?.Message ?? "Unable to load active services.");
            }

            var services = result.Data.Select(MapSummary).ToArray();
            if (!loadQuotes || services.Length == 0)
            {
                return ActiveServicesResult.Ok(services);
            }

            var quoteTasks = services
                .Select(service => GetQuoteOrNullAsync(service.Id, null, cancellationToken))
                .ToArray();
            var quotes = await Task.WhenAll(quoteTasks);

            for (var index = 0; index < services.Length; index++)
            {
                if (quotes[index] is not null)
                {
                    services[index] = services[index] with
                    {
                        PriceLabel = FormatIrr(quotes[index]!.FinalAmountIrr)
                    };
                }
            }

            return ActiveServicesResult.Ok(services);
        }
        catch (HttpRequestException)
        {
            return ActiveServicesResult.Fail("The service catalog API is unavailable.");
        }
        catch (System.Text.Json.JsonException)
        {
            return ActiveServicesResult.Fail("The service catalog response could not be read.");
        }
    }

    private async Task<PublicPricingQuote?> GetQuoteOrNullAsync(
        Guid serviceId,
        Guid? serviceVariantId,
        CancellationToken cancellationToken)
    {
        var client = _httpClientFactory.CreateClient(AppConstants.Api.HttpClientName);
        var endpoint = serviceVariantId.HasValue
            ? $"api/services/{serviceId}/quote?serviceVariantId={serviceVariantId.Value}"
            : $"api/services/{serviceId}/quote";

        try
        {
            var result = await client
                .GetFromJsonAsync<ServiceResult<PricingQuoteResponse>>(
                    endpoint,
                    cancellationToken);

            return result?.Success == true && result.Data is not null
                ? MapQuote(result.Data)
                : null;
        }
        catch (HttpRequestException)
        {
            return null;
        }
        catch (System.Text.Json.JsonException)
        {
            return null;
        }
    }

    private static PublicCatalogService MapSummary(ServiceSummaryResponse service)
    {
        return new PublicCatalogService(
            service.Id,
            service.CategoryId,
            service.CategoryName,
            Slugify(service.CategoryName),
            service.Name,
            service.Provider,
            GetProviderShortName(service.Provider),
            service.Slug,
            service.Description ?? "Managed digital-service request with payment and fulfillment tracking.",
            service.Description ?? "Managed digital-service request with payment and fulfillment tracking.",
            service.CurrencyCode,
            service.BasePrice,
            "Pricing unavailable",
            service.ProcessingTimeDescription ?? "Confirmed after payment");
    }

    private static PublicCatalogServiceDetail MapDetail(ServiceDetailResponse service)
    {
        var variants = service.Plans
            .Where(variant => variant.IsActive)
            .OrderBy(variant => variant.DisplayOrder)
            .Select(MapVariant)
            .ToArray();

        return new PublicCatalogServiceDetail(
            service.Id,
            service.CategoryId,
            service.CategoryName,
            Slugify(service.CategoryName),
            service.Name,
            service.Provider,
            GetProviderShortName(service.Provider),
            service.Slug,
            service.Description ?? "Managed digital-service request with payment and fulfillment tracking.",
            service.Description ?? "Managed digital-service request with payment and fulfillment tracking.",
            service.CurrencyCode,
            service.BasePrice,
            variants,
            service.RequiredFields
                .OrderBy(field => field.DisplayOrder)
                .Select(MapField)
                .ToArray(),
            service.ProcessingTimeDescription ?? variants.FirstOrDefault()?.ProcessingTime ?? "Confirmed after payment",
            service.SupportedFulfillmentModes.ToArray(),
            service.SupportsUseExistingAccount,
            service.SupportsCreateNewAccount);
    }

    private static PublicRequiredField MapField(ServiceRequiredFieldResponse field)
    {
        return new PublicRequiredField(
            field.FieldKey,
            field.Label,
            field.FieldType,
            field.FulfillmentMode,
            field.Actor,
            field.IsRequired,
            field.IsSensitive,
            field.DisplayOrder,
            field.OptionsJson,
            ParseFieldOptions(field.OptionsJson));
    }

    private static PublicCatalogVariant MapVariant(ServiceVariantResponse variant)
    {
        var billingPeriod = FormatBillingPeriod(variant.BillingCycle.ToString(), variant.DurationDays);
        return new PublicCatalogVariant(
            variant.Id.ToString(),
            variant.Id,
            variant.Name,
            $"{variant.RegionCode} {billingPeriod}".Trim(),
            $"{variant.BasePrice:N0} {variant.CurrencyCode}",
            variant.CurrencyCode,
            variant.BasePrice,
            billingPeriod,
            variant.ProcessingTimeDescription ?? "Confirmed after payment",
            variant.DisplayOrder == 0);
    }

    private static PublicPricingQuote MapQuote(PricingQuoteResponse quote)
    {
        return new PublicPricingQuote(
            quote.ServiceId,
            quote.ServiceVariantId ?? quote.PlanId,
            quote.PlanName,
            quote.CurrencyCode,
            quote.ForeignPrice,
            quote.ExchangeRate,
            quote.MarginPercentage,
            quote.FinalAmountIrr,
            quote.QuotedAtUtc);
    }

    private static string FormatBillingPeriod(string billingCycle, int? durationDays)
    {
        if (durationDays.HasValue && durationDays.Value > 0)
        {
            return $"{durationDays.Value} days";
        }

        return billingCycle == "None"
            ? "One-time"
            : billingCycle;
    }

    private static string FormatIrr(decimal amount)
    {
        return $"{amount:N0} IRR";
    }

    private static IReadOnlyList<PublicFieldOption> ParseFieldOptions(string? optionsJson)
    {
        if (string.IsNullOrWhiteSpace(optionsJson))
        {
            return [];
        }

        try
        {
            using var document = JsonDocument.Parse(optionsJson);
            if (document.RootElement.ValueKind != JsonValueKind.Array)
            {
                return [];
            }

            var options = new List<PublicFieldOption>();
            foreach (var item in document.RootElement.EnumerateArray())
            {
                var option = item.ValueKind switch
                {
                    JsonValueKind.String => BuildOption(item.GetString(), item.GetString()),
                    JsonValueKind.Object => BuildOption(
                        ReadString(item, "value") ?? ReadString(item, "id") ?? ReadString(item, "key"),
                        ReadString(item, "label") ?? ReadString(item, "name") ?? ReadString(item, "text")),
                    _ => null
                };

                if (option is not null)
                {
                    options.Add(option);
                }
            }

            return options;
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private static PublicFieldOption? BuildOption(string? value, string? label)
    {
        var normalizedValue = value?.Trim();
        if (string.IsNullOrWhiteSpace(normalizedValue))
        {
            return null;
        }

        var normalizedLabel = string.IsNullOrWhiteSpace(label)
            ? normalizedValue
            : label.Trim();

        return new PublicFieldOption(normalizedValue, normalizedLabel);
    }

    private static string? ReadString(JsonElement item, string propertyName)
    {
        return item.TryGetProperty(propertyName, out var property) &&
            property.ValueKind == JsonValueKind.String
            ? property.GetString()
            : null;
    }

    private static string GetProviderShortName(string provider)
    {
        var letters = provider
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(part => char.ToUpperInvariant(part[0]))
            .Take(2)
            .ToArray();

        return letters.Length == 0
            ? "SB"
            : new string(letters);
    }

    private static string Slugify(string value)
    {
        var lower = value.Trim().ToLowerInvariant();
        var slug = SlugUnsafeCharacters.Replace(lower, "-").Trim('-');
        return string.IsNullOrWhiteSpace(slug) ? "catalog" : slug;
    }
}

public sealed record PublicCatalogCategory(
    Guid Id,
    string Name,
    string Slug,
    string Description);

public sealed record PublicCatalogService(
    Guid Id,
    Guid CategoryId,
    string CategoryName,
    string CategorySlug,
    string Name,
    string ProviderName,
    string ProviderShortName,
    string Slug,
    string Description,
    string Summary,
    string CurrencyCode,
    decimal ForeignPrice,
    string PriceLabel,
    string ProcessingTime) : IServiceCardModel
{
    public string DetailHref => $"/ui/services/{Slug}";
}

public sealed record PublicCatalogServiceDetail(
    Guid Id,
    Guid CategoryId,
    string CategoryName,
    string CategorySlug,
    string Name,
    string ProviderName,
    string ProviderShortName,
    string Slug,
    string Description,
    string Summary,
    string CurrencyCode,
    decimal ForeignPrice,
    IReadOnlyList<PublicCatalogVariant> Variants,
    IReadOnlyList<PublicRequiredField> RequiredFields,
    string ProcessingTime,
    IReadOnlyList<OrderFulfillmentMode> SupportedFulfillmentModes,
    bool SupportsUseExistingAccount,
    bool SupportsCreateNewAccount) : IServiceCardModel
{
    public string DetailHref => $"/ui/services/{Slug}";
    public string PriceLabel => "Live quote";
}

public sealed record PublicCatalogVariant(
    string Id,
    Guid VariantId,
    string Name,
    string Description,
    string PriceText,
    string Currency,
    decimal ForeignPrice,
    string BillingPeriod,
    string ProcessingTime,
    bool IsPopular) : IServiceVariantOption
{
    public string MetaText => $"{Currency} {ForeignPrice:N0} {BillingPeriod}";
}

public sealed record PublicRequiredField(
    string FieldKey,
    string Label,
    string FieldType,
    OrderFulfillmentMode FulfillmentMode,
    CredentialActor Actor,
    bool IsRequired,
    bool IsSensitive,
    int DisplayOrder,
    string? OptionsJson,
    IReadOnlyList<PublicFieldOption> Options)
{
    public bool IsCustomerFieldFor(OrderFulfillmentMode fulfillmentMode)
    {
        return Actor == CredentialActor.Customer &&
            FulfillmentMode == fulfillmentMode;
    }
}

public sealed record PublicFieldOption(string Value, string Label);

public sealed record PublicPricingQuote(
    Guid ServiceId,
    Guid? ServiceVariantId,
    string? VariantName,
    string CurrencyCode,
    decimal ForeignPrice,
    decimal ExchangeRate,
    decimal MarginPercentage,
    decimal FinalAmountIrr,
    DateTime QuotedAtUtc);

public sealed record PublicCatalogResult(
    bool Success,
    string? ErrorMessage,
    IReadOnlyList<PublicCatalogCategory> Categories,
    IReadOnlyList<PublicCatalogService> Services)
{
    public static PublicCatalogResult Ok(
        IReadOnlyList<PublicCatalogCategory> categories,
        IReadOnlyList<PublicCatalogService> services)
    {
        return new PublicCatalogResult(true, null, categories, services);
    }

    public static PublicCatalogResult Fail(string? errorMessage)
    {
        return new PublicCatalogResult(false, errorMessage, [], []);
    }
}

public sealed record PublicServiceDetailResult(
    bool Success,
    bool NotFound,
    string? ErrorMessage,
    PublicCatalogServiceDetail? Service)
{
    public static PublicServiceDetailResult Ok(PublicCatalogServiceDetail service)
    {
        return new PublicServiceDetailResult(true, false, null, service);
    }

    public static PublicServiceDetailResult Fail(string? errorMessage)
    {
        return new PublicServiceDetailResult(false, false, errorMessage, null);
    }

    public static PublicServiceDetailResult Missing()
    {
        return new PublicServiceDetailResult(false, true, null, null);
    }
}

public sealed record PublicQuoteResult(
    bool Success,
    string? ErrorMessage,
    PublicPricingQuote? Quote)
{
    public static PublicQuoteResult Ok(PublicPricingQuote quote)
    {
        return new PublicQuoteResult(true, null, quote);
    }

    public static PublicQuoteResult Fail(string? errorMessage)
    {
        return new PublicQuoteResult(false, errorMessage, null);
    }
}

internal sealed record ActiveServicesResult(
    bool Success,
    string? ErrorMessage,
    PublicCatalogService[] Data)
{
    public static ActiveServicesResult Ok(PublicCatalogService[] data)
    {
        return new ActiveServicesResult(true, null, data);
    }

    public static ActiveServicesResult Fail(string? errorMessage)
    {
        return new ActiveServicesResult(false, errorMessage, []);
    }
}
