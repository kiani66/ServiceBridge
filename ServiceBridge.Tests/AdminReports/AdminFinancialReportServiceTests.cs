using ServiceBridge.Application.DTOs;
using ServiceBridge.Application.Interfaces;
using ServiceBridge.Domain.Entities;
using ServiceBridge.Domain.Enums;
using ServiceBridge.Infrastructure.Repositories;
using ServiceBridge.Infrastructure.Services;
using Xunit;

namespace ServiceBridge.Tests.AdminReports;

public class AdminFinancialReportServiceTests
{
    [Fact]
    public async Task GetFinancialReportAsync_RejectsInvalidDateRange()
    {
        var repository = new FakeAdminFinancialReportRepository();
        var service = new AdminFinancialReportService(repository);

        var result = await service.GetFinancialReportAsync(new AdminFinancialReportFilterDto
        {
            FromUtc = new DateTime(2026, 7, 2, 0, 0, 0, DateTimeKind.Utc),
            ToUtc = new DateTime(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc)
        });

        Assert.False(result.Success);
        Assert.Equal(400, result.StatusCode);
        Assert.Equal(0, repository.PaymentSummaryCalls);
    }

    [Fact]
    public async Task GetPaymentSummaryAsync_PassesStatusAndDateFilters()
    {
        var repository = new FakeAdminFinancialReportRepository();
        var service = new AdminFinancialReportService(repository);
        var from = new DateTime(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc);
        var to = new DateTime(2026, 7, 7, 23, 59, 59, DateTimeKind.Utc);

        var result = await service.GetPaymentSummaryAsync(new AdminFinancialReportFilterDto
        {
            FromUtc = from,
            ToUtc = to,
            PaymentStatus = PaymentStatus.Completed
        });

        Assert.True(result.Success);
        Assert.Equal(1, repository.PaymentSummaryCalls);
        Assert.Equal(from, repository.LastFilter!.FromUtc);
        Assert.Equal(to, repository.LastFilter.ToUtc);
        Assert.Equal(PaymentStatus.Completed, repository.LastFilter.PaymentStatus);
    }

    [Fact]
    public void PaymentFilters_ApplyDateRangeAndStatus()
    {
        var inRangeOrder = CreateOrder("SB-1001", new DateTime(2026, 7, 2, 0, 0, 0, DateTimeKind.Utc));
        var outOfRangeOrder = CreateOrder("SB-1002", new DateTime(2026, 6, 20, 0, 0, 0, DateTimeKind.Utc));
        var completed = CreatePayment(inRangeOrder, PaymentStatus.Completed, new DateTime(2026, 7, 3, 0, 0, 0, DateTimeKind.Utc));
        var pending = CreatePayment(inRangeOrder, PaymentStatus.Pending, new DateTime(2026, 7, 3, 0, 0, 0, DateTimeKind.Utc));
        var oldCompleted = CreatePayment(outOfRangeOrder, PaymentStatus.Completed, new DateTime(2026, 6, 21, 0, 0, 0, DateTimeKind.Utc));

        var results = AdminFinancialReportRepository.ApplyPaymentFilters(
            new[] { completed, pending, oldCompleted }.AsQueryable(),
            new AdminFinancialReportFilterDto
            {
                FromUtc = new DateTime(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc),
                ToUtc = new DateTime(2026, 7, 7, 0, 0, 0, DateTimeKind.Utc),
                PaymentStatus = PaymentStatus.Completed
            }).ToList();

        var payment = Assert.Single(results);
        Assert.Equal(completed.Id, payment.Id);
    }

    [Fact]
    public void RefundFilters_ApplyDateRangeAndStatus()
    {
        var requested = CreateRefund(
            RefundStatus.Requested,
            new DateTime(2026, 7, 2, 0, 0, 0, DateTimeKind.Utc));
        var approved = CreateRefund(
            RefundStatus.Approved,
            new DateTime(2026, 7, 3, 0, 0, 0, DateTimeKind.Utc));
        var oldApproved = CreateRefund(
            RefundStatus.Approved,
            new DateTime(2026, 6, 20, 0, 0, 0, DateTimeKind.Utc));

        var results = AdminFinancialReportRepository.ApplyRefundFilters(
            new[] { requested, approved, oldApproved }.AsQueryable(),
            new AdminFinancialReportFilterDto
            {
                FromUtc = new DateTime(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc),
                ToUtc = new DateTime(2026, 7, 7, 0, 0, 0, DateTimeKind.Utc),
                RefundStatus = RefundStatus.Approved
            }).ToList();

        var refund = Assert.Single(results);
        Assert.Equal(approved.Id, refund.Id);
    }

    [Fact]
    public async Task GetFinancialReportAsync_ReturnsCorrectAggregations()
    {
        var repository = new FakeAdminFinancialReportRepository
        {
            PaymentSummary = new AdminPaymentSummaryReportDto
            {
                TotalPayments = 4,
                CompletedPayments = 1,
                FailedPayments = 1,
                PendingPayments = 1,
                RefundedPayments = 1,
                TotalAmountIrr = 4_000_000m
            },
            OrderSummary = new AdminOrderFinancialSummaryReportDto
            {
                OrderCount = 2,
                TotalFinalPriceIrr = 3_500_000m,
                PaidAmountIrr = 3_000_000m,
                RefundedAmountIrr = 500_000m,
                EstimatedProfitIrr = 700_000m
            },
            RefundSummary = new AdminRefundSummaryReportDto
            {
                RefundCount = 2,
                RequestedRefunds = 1,
                CompletedRefunds = 1,
                TotalRefundAmountIrr = 500_000m
            }
        };
        var service = new AdminFinancialReportService(repository);

        var result = await service.GetFinancialReportAsync(new AdminFinancialReportFilterDto());

        Assert.True(result.Success);
        Assert.Equal(4, result.Data!.Payments.TotalPayments);
        Assert.Equal(3_000_000m, result.Data.Orders.PaidAmountIrr);
        Assert.Equal(500_000m, result.Data.Refunds.TotalRefundAmountIrr);
    }

    [Fact]
    public async Task ExportFinancialReportCsvAsync_IncludesRefundTotals()
    {
        var repository = new FakeAdminFinancialReportRepository
        {
            RefundSummary = new AdminRefundSummaryReportDto
            {
                RefundCount = 1,
                TotalRefundAmountIrr = 125_000m
            }
        };
        var service = new AdminFinancialReportService(repository);

        var result = await service.ExportFinancialReportCsvAsync(
            new AdminFinancialReportFilterDto());

        Assert.True(result.Success);
        Assert.Contains("Total refund amount IRR,125000", result.Data!.Content);
    }

    private sealed class FakeAdminFinancialReportRepository
        : IAdminFinancialReportRepository
    {
        public int PaymentSummaryCalls { get; private set; }
        public AdminFinancialReportFilterDto? LastFilter { get; private set; }
        public AdminPaymentSummaryReportDto PaymentSummary { get; set; } = new();
        public AdminOrderFinancialSummaryReportDto OrderSummary { get; set; } = new();
        public AdminRefundSummaryReportDto RefundSummary { get; set; } = new();

        public Task<AdminPaymentSummaryReportDto> GetPaymentSummaryAsync(
            AdminFinancialReportFilterDto filter)
        {
            PaymentSummaryCalls++;
            LastFilter = filter;
            return Task.FromResult(PaymentSummary);
        }

        public Task<AdminOrderFinancialSummaryReportDto>
            GetOrderFinancialSummaryAsync(AdminFinancialReportFilterDto filter)
        {
            LastFilter = filter;
            return Task.FromResult(OrderSummary);
        }

        public Task<AdminRefundSummaryReportDto> GetRefundSummaryAsync(
            AdminFinancialReportFilterDto filter)
        {
            LastFilter = filter;
            return Task.FromResult(RefundSummary);
        }
    }

    private static Order CreateOrder(string orderNumber, DateTime createdAt)
    {
        return new Order
        {
            Id = Guid.NewGuid(),
            OrderNumber = orderNumber,
            UserId = Guid.NewGuid(),
            Status = OrderStatus.WaitingForPayment,
            ForeignPrice = 10m,
            ExchangeRateSnapshot = 100_000m,
            MarginPercentSnapshot = 10m,
            BaseAmountIrr = 1_000_000m,
            ProfitAmountIrr = 100_000m,
            FinalAmountIrr = 1_100_000m,
            CreatedAt = createdAt
        };
    }

    private static Payment CreatePayment(
        Order order,
        PaymentStatus status,
        DateTime occurredAt)
    {
        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            OrderId = order.Id,
            Order = order,
            AmountIRR = order.FinalAmountIrr,
            AmountUSD = order.ForeignPrice,
            ExchangeRateSnapshot = order.ExchangeRateSnapshot,
            CreatedAt = occurredAt
        };

        switch (status)
        {
            case PaymentStatus.Completed:
                payment.Complete("gateway", "{}", occurredAt);
                break;
            case PaymentStatus.Failed:
                payment.Fail("gateway", "{}", "Gateway failed.", occurredAt);
                break;
            case PaymentStatus.Refunded:
                payment.Complete("gateway", "{}", occurredAt.AddMinutes(-1));
                payment.Refund("refund", "{}", "Refunded.", occurredAt);
                break;
        }

        return payment;
    }

    private static RefundRequest CreateRefund(
        RefundStatus status,
        DateTime createdAt)
    {
        var refund = RefundRequest.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            125_000m,
            "Customer request.",
            createdAt);

        switch (status)
        {
            case RefundStatus.Approved:
                refund.Approve(Guid.NewGuid(), "Approved.", createdAt.AddMinutes(1));
                break;
            case RefundStatus.Rejected:
                refund.Reject(Guid.NewGuid(), "Rejected.", createdAt.AddMinutes(1));
                break;
            case RefundStatus.Completed:
                var adminId = Guid.NewGuid();
                refund.Approve(adminId, "Approved.", createdAt.AddMinutes(1));
                refund.Complete(adminId, "Completed.", createdAt.AddMinutes(2));
                break;
            case RefundStatus.Cancelled:
                refund.Cancel(Guid.NewGuid(), createdAt.AddMinutes(1));
                break;
        }

        return refund;
    }
}
