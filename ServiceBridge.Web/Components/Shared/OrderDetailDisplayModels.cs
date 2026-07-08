namespace ServiceBridge.Web.Components.Shared;

public sealed record OrderFact(
    string Label,
    string Value,
    string? BadgeTone = null,
    string? BadgeIcon = null);

public sealed record CredentialDisplayItem(
    string Label,
    string Value,
    bool IsSensitive,
    bool IsVisibleToCustomerAfterCompletion = false);

public sealed record NoteDisplayItem(
    string Text,
    string Author,
    string CreatedAt,
    string Visibility);
