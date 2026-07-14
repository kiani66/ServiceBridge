using Bunit;
using MudBlazor.Services;
using ServiceBridge.Application.DTOs;
using ServiceBridge.Domain.Enums;
using ServiceBridge.Domain.Orders;
using ServiceBridge.Web.Components.Shared;
using ServiceBridge.Web.Services;
using Xunit;

namespace ServiceBridge.Tests.Web;

public class OrderProgressAndActivityTests : BunitContext
{
    public OrderProgressAndActivityTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddMudServices(options =>
            options.PopoverOptions.CheckForPopoverProvider = false);
    }

    [Theory]
    [InlineData(OrderStatus.Created, 0)]
    [InlineData(OrderStatus.WaitingForPayment, 1)]
    [InlineData(OrderStatus.Paid, 2)]
    [InlineData(OrderStatus.Assigned, 3)]
    [InlineData(OrderStatus.Processing, 4)]
    [InlineData(OrderStatus.Completed, 5)]
    public void NormalProgress_UsesTheActualLifecycleAndCurrentStatus(
        OrderStatus status,
        int currentIndex)
    {
        var stages = OrderProgress.For(status.ToString());

        Assert.Equal(
            new[]
            {
                nameof(OrderStatus.Created),
                nameof(OrderStatus.WaitingForPayment),
                nameof(OrderStatus.Paid),
                nameof(OrderStatus.Assigned),
                nameof(OrderStatus.Processing),
                nameof(OrderStatus.Completed)
            },
            stages.Select(stage => stage.Status));
        Assert.Equal("Waiting for payment", stages[1].Label);
        Assert.All(
            stages.Take(currentIndex),
            stage => Assert.Equal(OrderProgressStageState.Complete, stage.State));
        Assert.Equal(OrderProgressStageState.Current, stages[currentIndex].State);
        Assert.All(
            stages.Skip(currentIndex + 1),
            stage => Assert.Equal(OrderProgressStageState.Future, stage.State));
    }

    [Fact]
    public void CompletedProgress_ShowsASuccessfulTerminalCurrentStep()
    {
        var stages = OrderProgress.For(nameof(OrderStatus.Completed));
        var current = Assert.Single(stages.Where(stage =>
            stage.State == OrderProgressStageState.Current));

        Assert.Equal(nameof(OrderStatus.Completed), current.Status);
        Assert.Equal("success", current.Tone);
        Assert.DoesNotContain(stages, stage => stage.State == OrderProgressStageState.Future);
    }

    [Fact]
    public void ProgressPaths_MatchDomainTransitionRules()
    {
        var normalPath = OrderProgress.For(nameof(OrderStatus.Created))
            .Select(stage => Enum.Parse<OrderStatus>(stage.Status))
            .ToArray();

        for (var index = 0; index < normalPath.Length - 1; index++)
        {
            Assert.True(OrderStatusTransitions.CanTransition(
                normalPath[index],
                normalPath[index + 1]));
        }

        Assert.True(OrderStatusTransitions.CanTransition(
            OrderStatus.Processing,
            OrderStatus.Failed));
        Assert.True(OrderStatusTransitions.IsTerminal(OrderStatus.Completed));
        Assert.True(OrderStatusTransitions.IsTerminal(OrderStatus.Failed));
        Assert.True(OrderStatusTransitions.IsTerminal(OrderStatus.Cancelled));
    }

    [Fact]
    public void FailedProgress_ReplacesSuccessfulCompletionWithFailedTerminalStep()
    {
        var stages = OrderProgress.For(nameof(OrderStatus.Failed));
        var current = Assert.Single(stages.Where(stage =>
            stage.State == OrderProgressStageState.Current));

        Assert.Equal(nameof(OrderStatus.Failed), current.Status);
        Assert.Equal("danger", current.Tone);
        Assert.DoesNotContain(stages, stage => stage.Status == nameof(OrderStatus.Completed));
        Assert.Equal(
            OrderProgressStageState.Complete,
            Assert.Single(stages.Where(stage =>
                stage.Status == nameof(OrderStatus.Processing))).State);
    }

    [Fact]
    public void CancelledProgress_DoesNotClaimUnknownIntermediateStepsOrCompletion()
    {
        var stages = OrderProgress.For(nameof(OrderStatus.Cancelled));

        Assert.Equal(
            new[] { nameof(OrderStatus.Created), nameof(OrderStatus.Cancelled) },
            stages.Select(stage => stage.Status));
        Assert.Equal(OrderProgressStageState.Complete, stages[0].State);
        Assert.Equal(OrderProgressStageState.Current, stages[1].State);
        Assert.Equal("danger", stages[1].Tone);
    }

    [Theory]
    [InlineData("RequiresCustomerAction")]
    [InlineData("RefundPending")]
    [InlineData("Refunded")]
    public void NonOrderWorkflowNames_FallBackWithoutInventingSuccessfulProgress(
        string status)
    {
        Assert.DoesNotContain(status, Enum.GetNames<OrderStatus>());

        var stage = Assert.Single(OrderProgress.For(status));
        Assert.Equal(OrderProgressStageState.Current, stage.State);
        Assert.Equal("neutral", stage.Tone);
        Assert.NotEqual(nameof(OrderStatus.Completed), stage.Status);
    }

    [Fact]
    public void ActivityHistory_PreservesRepeatedStatusesOrderDescriptionsAndTimestamps()
    {
        var firstTime = new DateTime(2026, 7, 13, 8, 0, 0, DateTimeKind.Utc);
        var secondTime = firstTime.AddMinutes(10);
        var history = new[]
        {
            new OrderStatusHistoryDto
            {
                PreviousStatus = nameof(OrderStatus.Assigned),
                Status = nameof(OrderStatus.Processing),
                CreatedAt = firstTime
            },
            new OrderStatusHistoryDto
            {
                PreviousStatus = nameof(OrderStatus.Processing),
                Status = nameof(OrderStatus.Processing),
                CreatedAt = secondTime
            }
        };

        var items = UserOrderDisplay.TimelineItems(history);

        Assert.Equal(2, items.Count);
        Assert.All(items, item => Assert.Equal("Processing", item.Title));
        Assert.Equal("Assigned changed to Processing.", items[0].Description);
        Assert.Equal("Processing changed to Processing.", items[1].Description);
        Assert.Equal(UserOrderDisplay.FormatDateTime(firstTime), items[0].Time);
        Assert.Equal(UserOrderDisplay.FormatDateTime(secondTime), items[1].Time);
    }

    [Fact]
    public void OrderTimeline_RendersSeparateProgressAndAppendOnlyActivity()
    {
        var items = new[]
        {
            new TimelineItem("Processing", "Assigned changed to Processing.", "2026-07-13 08:00", "info"),
            new TimelineItem("Processing", "Processing changed to Processing.", "2026-07-13 08:10", "info")
        };
        var cut = Render<OrderTimeline>(parameters => parameters
            .Add(component => component.CurrentStatus, nameof(OrderStatus.Processing))
            .Add(component => component.Items, items));

        Assert.Equal(
            ["Order progress", "Activity history"],
            cut.FindAll("h2").Select(heading => heading.TextContent.Trim()));
        Assert.Single(cut.FindAll("ol[aria-label='Order progress']"));
        Assert.Equal(2, cut.FindAll("ol[aria-label='Activity history'] li").Count);
        Assert.Equal(2, cut.FindAll("ol[aria-label='Activity history'] strong").Count(item =>
            item.TextContent.Trim().Equals("Processing", StringComparison.Ordinal)));
        Assert.Contains("Assigned changed to Processing.", cut.Markup);
        Assert.Contains("Processing changed to Processing.", cut.Markup);
        Assert.Contains("2026-07-13 08:00", cut.Markup);
        Assert.Contains("2026-07-13 08:10", cut.Markup);
    }

    [Theory]
    [InlineData(nameof(OrderStatus.Failed), "Failed")]
    [InlineData(nameof(OrderStatus.Cancelled), "Cancelled")]
    public void TerminalProgress_RendersCurrentTerminalState(string status, string label)
    {
        var cut = Render<OrderLifecycle>(parameters => parameters
            .Add(component => component.CurrentStatus, status));

        Assert.Equal(label, cut.Find("[aria-current='step'] strong").TextContent.Trim());
        Assert.DoesNotContain("Completed", cut.Markup);
    }

    [Fact]
    public void OrderDetailPageComposition_StaticArchitectureGuard()
    {
        // A shared component invocation across three Razor pages is a composition convention.
        foreach (var page in new[]
                 {
                     "OrderDetails.razor",
                     "AgentOrderWorkspace.razor",
                     "AdminOrderManagement.razor"
                 })
        {
            var source = File.ReadAllText(WebPath("Components", "Pages", page));
            Assert.Equal(1, source.Split("<OrderTimeline", StringSplitOptions.None).Length - 1);
        }
    }

    [Fact]
    public void HistoryProjections_StaticArchitectureGuard()
    {
        // Query ordering and duplicate preservation span repository projections and are impractical to prove from one UI render.
        foreach (var repository in new[]
                 {
                     "UserOrderQueryRepository.cs",
                     "AgentOrderQueryRepository.cs",
                     "AdminOrderQueryRepository.cs"
                 })
        {
            var source = File.ReadAllText(InfrastructurePath("Repositories", repository));
            var historyProjection = HistoryProjection(source);

            Assert.Contains(".OrderBy(x => x.CreatedAt)", historyProjection);
            Assert.Contains(".ThenBy(x => x.Id)", historyProjection);
            Assert.DoesNotContain("Distinct", historyProjection);
            Assert.DoesNotContain("GroupBy", historyProjection);
        }

        var display = File.ReadAllText(WebPath("Services", "UserOrderDisplay.cs"));
        Assert.DoesNotContain("Distinct", display);
        Assert.DoesNotContain("GroupBy", display);
        Assert.DoesNotContain("OrderBy(item => item.CreatedAt)", display);
    }

    private static string HistoryProjection(string source)
    {
        var start = source.IndexOf(".StatusHistory =", StringComparison.Ordinal);
        var end = source.IndexOf(".ToListAsync();", start, StringComparison.Ordinal);
        return source[start..(end + ".ToListAsync();".Length)];
    }

    private static string WebPath(params string[] parts)
    {
        return ProjectPath("ServiceBridge.Web", parts);
    }

    private static string InfrastructurePath(params string[] parts)
    {
        return ProjectPath("ServiceBridge.Infrastructure", parts);
    }

    private static string ProjectPath(string project, params string[] parts)
    {
        var root = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..",
            "..",
            "..",
            ".."));
        return Path.Combine([root, project, .. parts]);
    }
}
