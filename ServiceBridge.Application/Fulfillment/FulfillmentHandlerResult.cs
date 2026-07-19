using ServiceBridge.Domain.ValueObjects;

namespace ServiceBridge.Application.Fulfillment;

public sealed class FulfillmentHandlerResult
{
    public const int FailureMessageMaxLength = 500;

    private FulfillmentHandlerResult(
        FulfillmentHandlerOutcome outcome,
        FulfillmentFailureCode? failureCode,
        string? failureMessage)
    {
        Outcome = outcome;
        FailureCode = failureCode;
        FailureMessage = SanitizeFailureMessage(failureMessage);
    }

    public FulfillmentHandlerOutcome Outcome { get; }
    public FulfillmentFailureCode? FailureCode { get; }
    public string? FailureMessage { get; }

    public static FulfillmentHandlerResult Succeeded()
    {
        return new FulfillmentHandlerResult(
            FulfillmentHandlerOutcome.Succeeded,
            failureCode: null,
            failureMessage: null);
    }

    public static FulfillmentHandlerResult RetryableFailure(
        FulfillmentFailureCode failureCode,
        string? failureMessage = null)
    {
        ArgumentNullException.ThrowIfNull(failureCode);
        return new FulfillmentHandlerResult(
            FulfillmentHandlerOutcome.RetryableFailure,
            failureCode,
            failureMessage);
    }

    public static FulfillmentHandlerResult TerminalFailure(
        FulfillmentFailureCode failureCode,
        string? failureMessage = null)
    {
        ArgumentNullException.ThrowIfNull(failureCode);
        return new FulfillmentHandlerResult(
            FulfillmentHandlerOutcome.TerminalFailure,
            failureCode,
            failureMessage);
    }

    private static string? SanitizeFailureMessage(string? failureMessage)
    {
        if (string.IsNullOrWhiteSpace(failureMessage))
        {
            return null;
        }

        var sanitized = string.Join(
            ' ',
            failureMessage.Split(
                (char[]?)null,
                StringSplitOptions.RemoveEmptyEntries));

        if (sanitized.Length > FailureMessageMaxLength)
        {
            throw new ArgumentOutOfRangeException(
                nameof(failureMessage),
                $"Failure message cannot exceed {FailureMessageMaxLength} characters.");
        }

        return sanitized;
    }
}
