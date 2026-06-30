using MudBlazor;
using ServiceBridge.Web.Components.Shared;

namespace ServiceBridge.Web.MockData;

public sealed class ServiceBridgeMockDataProvider
{
    public IReadOnlyList<ProviderModel> Providers { get; }
    public IReadOnlyList<ServiceCategoryModel> Categories { get; }
    public IReadOnlyList<DigitalServiceModel> Services { get; }
    public IReadOnlyList<OrderModel> Orders { get; }
    public IReadOnlyList<MetricModel> DashboardMetrics { get; }
    public IReadOnlyList<ActivityModel> Activity { get; }

    public ServiceBridgeMockDataProvider()
    {
        Providers =
        [
            new("openai", "OpenAI", "AI productivity", "OP", "info"),
            new("anthropic", "Anthropic", "AI productivity", "AN", "warning"),
            new("cursor", "Cursor", "Developer tools", "CU", "success"),
            new("github", "GitHub", "Developer tools", "GH", "neutral")
        ];

        Categories =
        [
            new("ai-assistants", "AI assistants", "Individual and team subscriptions for language-model products.", Icons.Material.Outlined.AutoAwesome),
            new("developer-tools", "Developer tools", "Coding, repository, and productivity tools for software teams.", Icons.Material.Outlined.Code),
            new("business-plans", "Business plans", "Managed team and enterprise plans with billing and fulfillment guidance.", Icons.Material.Outlined.BusinessCenter)
        ];

        Services =
        [
            new(
                "chatgpt-plus",
                "ChatGPT Plus",
                "OpenAI",
                "OP",
                "ai-assistants",
                "AI assistants",
                "Access GPT-4o, advanced tools, file analysis, and priority capacity through a managed subscription flow.",
                "Best for individual AI research, writing, analysis, and coding assistance.",
                Icons.Material.Outlined.AutoAwesome,
                true,
                ["Account email", "Region", "Recovery contact"],
                [
                    new("chatgpt-monthly", "Monthly", "Personal monthly plan", 20, "USD", "per month", 6, 690000, "24 to 48 hours", true),
                    new("chatgpt-yearly", "Yearly", "Prepaid annual access", 220, "USD", "per year", 5, 690000, "24 to 48 hours", false),
                    new("chatgpt-business", "Business", "Team workspace setup", 30, "USD", "per user/month", 8, 690000, "2 to 4 business days", false)
                ]),
            new(
                "claude-pro",
                "Claude Pro",
                "Anthropic",
                "AN",
                "ai-assistants",
                "AI assistants",
                "Managed Claude Pro activation for research, long-context writing, and analysis workflows.",
                "Best for long-form drafting, document reasoning, and knowledge work.",
                Icons.Material.Outlined.Psychology,
                true,
                ["Account email", "Country profile", "Backup contact"],
                [
                    new("claude-monthly", "Monthly", "Personal monthly plan", 20, "USD", "per month", 6, 690000, "24 to 48 hours", true),
                    new("claude-yearly", "Yearly", "Annual subscription", 200, "USD", "per year", 5, 690000, "24 to 72 hours", false),
                    new("claude-enterprise", "Enterprise", "Procurement-guided team setup", 1200, "USD", "starting package", 10, 690000, "5 to 7 business days", false)
                ]),
            new(
                "cursor-pro",
                "Cursor Pro",
                "Cursor",
                "CU",
                "developer-tools",
                "Developer tools",
                "AI-native editor subscription setup with account and billing coordination handled by ServiceBridge.",
                "Best for developers who want integrated AI coding assistance.",
                Icons.Material.Outlined.Terminal,
                false,
                ["Account email", "Preferred billing cycle", "Team name"],
                [
                    new("cursor-monthly", "Monthly", "Individual plan", 20, "USD", "per month", 6, 690000, "24 hours", true),
                    new("cursor-yearly", "Yearly", "Annual individual plan", 192, "USD", "per year", 5, 690000, "24 hours", false),
                    new("cursor-business", "Business", "Team seats", 40, "USD", "per user/month", 8, 690000, "2 business days", false)
                ]),
            new(
                "github-copilot",
                "GitHub Copilot",
                "GitHub",
                "GH",
                "developer-tools",
                "Developer tools",
                "Copilot subscription setup for individual developers and teams with clear payment and fulfillment tracking.",
                "Best for code completion, chat, and repository-aware developer workflows.",
                Icons.Material.Outlined.Hub,
                false,
                ["GitHub username", "Account email", "Organization name"],
                [
                    new("copilot-monthly", "Monthly", "Individual plan", 10, "USD", "per month", 6, 690000, "24 hours", true),
                    new("copilot-yearly", "Yearly", "Annual individual plan", 100, "USD", "per year", 5, 690000, "24 hours", false),
                    new("copilot-business", "Business", "Organization seats", 19, "USD", "per user/month", 8, 690000, "2 business days", false),
                    new("copilot-enterprise", "Enterprise", "Enterprise seats", 39, "USD", "per user/month", 10, 690000, "3 to 5 business days", false)
                ])
        ];

        Orders =
        [
            new("SB-1048", "ChatGPT Plus", "OpenAI", "Monthly", "Arian Kiani", "arian@example.com", "Pending", "Awaiting payment", "Unassigned", DateTimeOffset.Now.AddHours(-8), DateTimeOffset.Now.AddHours(-2), 14628000, true),
            new("SB-1047", "Claude Pro", "Anthropic", "Yearly", "Neda Karimi", "neda@example.com", "Processing", "Paid", "Sara Mohammadi", DateTimeOffset.Now.AddDays(-1), DateTimeOffset.Now.AddHours(-1), 144900000, true),
            new("SB-1046", "Cursor Pro", "Cursor", "Business", "Kaveh Team", "ops@kaveh.example", "Completed", "Paid", "Reza Ahmadi", DateTimeOffset.Now.AddDays(-3), DateTimeOffset.Now.AddDays(-1), 29808000, false),
            new("SB-1045", "GitHub Copilot", "GitHub", "Enterprise", "Atlas Labs", "admin@atlas.example", "Failed", "Payment failed", "Unassigned", DateTimeOffset.Now.AddDays(-4), DateTimeOffset.Now.AddDays(-4).AddHours(2), 29601000, false),
            new("SB-1044", "GitHub Copilot", "GitHub", "Monthly", "Mina Rostami", "mina@example.com", "Processing", "Paid", "Omid Farhadi", DateTimeOffset.Now.AddDays(-5), DateTimeOffset.Now.AddHours(-6), 7314000, false)
        ];

        DashboardMetrics =
        [
            new("Awaiting payment", "12", "Orders need user payment", "warning", Icons.Material.Outlined.Payments),
            new("Paid unassigned", "7", "Ready for admin assignment", "info", Icons.Material.Outlined.AssignmentInd),
            new("In fulfillment", "18", "Agent work currently active", "neutral", Icons.Material.Outlined.ManageHistory),
            new("Completed today", "9", "Fulfilled service requests", "success", Icons.Material.Outlined.CheckCircle)
        ];

        Activity =
        [
            new("Payment verified", "SB-1047 payment snapshot captured and order moved to fulfillment.", "2 hours ago", "success"),
            new("Credential review", "SB-1048 requires credential confirmation before assignment.", "4 hours ago", "warning"),
            new("Agent update", "SB-1044 waiting on provider-side activation confirmation.", "6 hours ago", "info"),
            new("Order completed", "SB-1046 delivered Cursor Business seats to the customer.", "Yesterday", "success")
        ];
    }

    public DigitalServiceModel FeaturedService => Services[0];

    public DigitalServiceModel? FindService(string? slug)
    {
        return Services.FirstOrDefault(service => string.Equals(service.Slug, slug, StringComparison.OrdinalIgnoreCase));
    }

    public ServiceCategoryModel? FindCategory(string? slug)
    {
        return Categories.FirstOrDefault(category => string.Equals(category.Slug, slug, StringComparison.OrdinalIgnoreCase));
    }

    public OrderModel? FindOrder(string? orderNumber)
    {
        return Orders.FirstOrDefault(order => string.Equals(order.OrderNumber, orderNumber, StringComparison.OrdinalIgnoreCase));
    }

    public IReadOnlyList<DigitalServiceModel> ServicesForCategory(string? categorySlug)
    {
        return Services
            .Where(service => string.Equals(service.CategorySlug, categorySlug, StringComparison.OrdinalIgnoreCase))
            .ToArray();
    }
}

public sealed record ProviderModel(string Slug, string Name, string Category, string ShortName, string Tone);

public sealed record ServiceCategoryModel(string Slug, string Name, string Description, string Icon);

public sealed record DigitalServiceModel(
    string Slug,
    string Name,
    string ProviderName,
    string ProviderShortName,
    string CategorySlug,
    string CategoryName,
    string Description,
    string Summary,
    string Icon,
    bool RequiresCredentials,
    IReadOnlyList<string> RequiredFields,
    IReadOnlyList<ServiceVariantModel> Variants) : IServiceCardModel
{
    public ServiceVariantModel PrimaryVariant => Variants[0];
    public string DetailHref => $"/ui/services/{Slug}";
    public string PriceLabel => $"{PrimaryVariant.FinalPriceIrr:N0} IRR";
    public string ProcessingTime => PrimaryVariant.ProcessingTime;
}

public sealed record ServiceVariantModel(
    string Id,
    string Name,
    string Description,
    decimal ForeignPrice,
    string Currency,
    string BillingPeriod,
    decimal MarginPercent,
    decimal ExchangeRate,
    string ProcessingTime,
    bool IsPopular) : IServiceVariantOption
{
    public decimal FinalPriceIrr => Math.Round(ForeignPrice * ExchangeRate * (1 + MarginPercent / 100), 0);
    public string PriceText => $"{FinalPriceIrr:N0} IRR";
    public string MetaText => $"{Currency} {ForeignPrice:N0} {BillingPeriod}";
}

public sealed record OrderModel(
    string OrderNumber,
    string ServiceName,
    string ProviderName,
    string VariantName,
    string CustomerName,
    string CustomerEmail,
    string Status,
    string PaymentStatus,
    string Assignment,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    decimal TotalIrr,
    bool RequiresCredentialReview);

public sealed record MetricModel(string Label, string Value, string Description, string Tone, string Icon);

public sealed record ActivityModel(string Title, string Description, string Time, string Tone);
