using ServiceBridge.Domain.Enums;

namespace ServiceBridge.Web.Components.Shared;

public static class OrderProgress
{
    private static readonly OrderStatus[] NormalPath =
    [
        OrderStatus.Created,
        OrderStatus.WaitingForPayment,
        OrderStatus.Paid,
        OrderStatus.Assigned,
        OrderStatus.Processing,
        OrderStatus.Completed
    ];

    public static IReadOnlyList<OrderProgressStage> For(string currentStatus)
    {
        if (!Enum.TryParse<OrderStatus>(currentStatus, out var parsed) ||
            !Enum.IsDefined(parsed))
        {
            return
            [
                new OrderProgressStage(
                    currentStatus,
                    Humanize(currentStatus),
                    OrderProgressStageState.Current,
                    "neutral")
            ];
        }

        return parsed switch
        {
            OrderStatus.Cancelled => CancelledPath(),
            OrderStatus.Failed => FailedPath(),
            _ => NormalProgress(parsed)
        };
    }

    private static IReadOnlyList<OrderProgressStage> NormalProgress(
        OrderStatus currentStatus)
    {
        var currentIndex = Array.IndexOf(NormalPath, currentStatus);
        return NormalPath
            .Select((status, index) => Stage(
                status,
                index < currentIndex
                    ? OrderProgressStageState.Complete
                    : index == currentIndex
                        ? OrderProgressStageState.Current
                        : OrderProgressStageState.Future,
                status == OrderStatus.Completed ? "success" : "info"))
            .ToArray();
    }

    private static IReadOnlyList<OrderProgressStage> FailedPath()
    {
        return NormalPath
            .TakeWhile(status => status != OrderStatus.Completed)
            .Select(status => Stage(
                status,
                OrderProgressStageState.Complete,
                "info"))
            .Append(Stage(
                OrderStatus.Failed,
                OrderProgressStageState.Current,
                "danger"))
            .ToArray();
    }

    private static IReadOnlyList<OrderProgressStage> CancelledPath()
    {
        return
        [
            Stage(
                OrderStatus.Created,
                OrderProgressStageState.Complete,
                "info"),
            Stage(
                OrderStatus.Cancelled,
                OrderProgressStageState.Current,
                "danger")
        ];
    }

    private static OrderProgressStage Stage(
        OrderStatus status,
        OrderProgressStageState state,
        string tone)
    {
        return new OrderProgressStage(
            status.ToString(),
            status == OrderStatus.WaitingForPayment
                ? "Waiting for payment"
                : Humanize(status.ToString()),
            state,
            tone);
    }

    private static string Humanize(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "Unknown status";
        }

        return string.Concat(value.Select((character, index) =>
            index > 0 && char.IsUpper(character)
                ? $" {character}"
                : character.ToString()));
    }
}

public enum OrderProgressStageState
{
    Complete,
    Current,
    Future
}

public sealed record OrderProgressStage(
    string Status,
    string Label,
    OrderProgressStageState State,
    string Tone);
