using ServiceBridge.Application.DTOs;
using ServiceBridge.Application.Interfaces;
using ServiceBridge.Domain.Entities;
using ServiceBridge.Domain.Enums;
using ServiceBridge.Infrastructure.Services;
using ServiceBridge.Tests.TestDoubles;
using Xunit;

namespace ServiceBridge.Tests.SupportTickets;

public class TicketServiceTests
{
    [Fact]
    public async Task CreateTicketAsync_WhenRequestIsValid_CreatesTicket()
    {
        var userId = Guid.NewGuid();
        var repository = new FakeTicketRepository
        {
            ExistingUserIds = { userId }
        };
        var service = CreateService(repository, new FakeTicketNumberGenerator("TKT-1"));

        var result = await service.CreateTicketAsync(
            userId,
            new CreateTicketRequest
            {
                Subject = "Payment question",
                Body = "I need support."
            });

        Assert.True(result.Success);
        Assert.Equal(201, result.StatusCode);
        Assert.NotNull(result.Data);
        Assert.Equal("TKT-1", result.Data.TicketNumber);
        Assert.Equal(nameof(TicketStatus.WaitingForSupport), result.Data.Status);
        Assert.Single(repository.Tickets);
    }

    [Fact]
    public async Task CreateTicketAsync_WhenOrderBelongsToAnotherUser_ReturnsValidationFailure()
    {
        var userId = Guid.NewGuid();
        var otherOrderId = Guid.NewGuid();
        var repository = new FakeTicketRepository
        {
            ExistingUserIds = { userId }
        };
        var service = CreateService(repository, new FakeTicketNumberGenerator("TKT-1"));

        var result = await service.CreateTicketAsync(
            userId,
            new CreateTicketRequest
            {
                OrderId = otherOrderId,
                Subject = "Order question",
                Body = "Please check this order."
            });

        Assert.False(result.Success);
        Assert.Equal(400, result.StatusCode);
        Assert.NotNull(result.ValidationErrors);
        Assert.Contains(nameof(CreateTicketRequest.OrderId), result.ValidationErrors);
        Assert.Empty(repository.Tickets);
    }

    [Fact]
    public void CreateTicketRequest_DoesNotExposeCategoryInput()
    {
        Assert.Null(typeof(CreateTicketRequest).GetProperty("CategoryId"));
    }

    [Theory]
    [InlineData("CategoryId")]
    [InlineData("CategoryName")]
    [InlineData("AssignedAgentId")]
    [InlineData("AssignedAgentName")]
    [InlineData("ResolvedAt")]
    public void TicketDetailDto_DoesNotExposeOutOfScopeTicketFields(
        string propertyName)
    {
        Assert.Null(typeof(TicketDetailDto).GetProperty(propertyName));
    }

    [Fact]
    public async Task GetMyTicketAsync_DoesNotExposeInternalMessages()
    {
        var userId = Guid.NewGuid();
        var adminId = Guid.NewGuid();
        var ticket = CreateTicket(userId);
        ticket.AddAdminMessage(adminId, "Internal note", true, DateTime.UtcNow);
        ticket.AddAdminMessage(adminId, "Public reply", false, DateTime.UtcNow);
        var repository = new FakeTicketRepository
        {
            ExistingUserIds = { userId },
            Tickets = { ticket }
        };
        var service = CreateService(repository, new FakeTicketNumberGenerator("TKT-2"));

        var result = await service.GetMyTicketAsync(userId, ticket.Id);

        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.DoesNotContain(result.Data.Messages, message => message.IsInternal);
        Assert.Contains(result.Data.Messages, message => message.Body == "Public reply");
    }

    [Fact]
    public async Task AddUserMessageAsync_ChangesStatusToWaitingForSupport()
    {
        var userId = Guid.NewGuid();
        var ticket = CreateTicket(userId);
        ticket.AddAdminMessage(Guid.NewGuid(), "Public reply", false, DateTime.UtcNow);
        var repository = new FakeTicketRepository
        {
            ExistingUserIds = { userId },
            Tickets = { ticket }
        };
        var service = CreateService(repository, new FakeTicketNumberGenerator("TKT-2"));

        var result = await service.AddUserMessageAsync(
            userId,
            ticket.Id,
            new CreateTicketMessageRequest { Body = "Customer reply" });

        Assert.True(result.Success);
        Assert.Equal(TicketStatus.WaitingForSupport, ticket.Status);
        Assert.Contains(ticket.Messages, message => message.Body == "Customer reply");
    }

    [Fact]
    public async Task AddAdminMessageAsync_PublicReply_ChangesStatusToWaitingForUser()
    {
        var userId = Guid.NewGuid();
        var ticket = CreateTicket(userId);
        var repository = new FakeTicketRepository
        {
            ExistingUserIds = { userId },
            Tickets = { ticket }
        };
        var service = CreateService(repository, new FakeTicketNumberGenerator("TKT-2"));

        var result = await service.AddAdminMessageAsync(
            Guid.NewGuid(),
            ticket.Id,
            new CreateTicketMessageRequest { Body = "Support reply" });

        Assert.True(result.Success);
        Assert.Equal(TicketStatus.WaitingForUser, ticket.Status);
        Assert.NotNull(ticket.FirstResponseAt);
    }

    [Fact]
    public async Task AddUserMessageAsync_QueuesAdminNotification()
    {
        var userId = Guid.NewGuid();
        var ticket = CreateTicket(userId);
        var repository = new FakeTicketRepository
        {
            ExistingUserIds = { userId },
            Tickets = { ticket }
        };
        var notificationService = new FakeNotificationService();
        var service = CreateService(
            repository,
            new FakeTicketNumberGenerator("TKT-2"),
            notificationService);

        var result = await service.AddUserMessageAsync(
            userId,
            ticket.Id,
            new CreateTicketMessageRequest { Body = "Customer reply" });

        Assert.True(result.Success);
        Assert.Contains(
            nameof(FakeNotificationService.QueueTicketUserReplyForAdminsAsync),
            notificationService.QueuedTypes);
        Assert.Contains(
            nameof(FakeNotificationService.PublishQueuedRealtimeAsync),
            notificationService.QueuedTypes);
    }

    [Fact]
    public async Task AddAdminMessageAsync_InternalMessage_DoesNotExposeToUser()
    {
        var userId = Guid.NewGuid();
        var ticket = CreateTicket(userId);
        var repository = new FakeTicketRepository
        {
            ExistingUserIds = { userId },
            Tickets = { ticket }
        };
        var service = CreateService(repository, new FakeTicketNumberGenerator("TKT-2"));

        var result = await service.AddAdminMessageAsync(
            Guid.NewGuid(),
            ticket.Id,
            new CreateTicketMessageRequest
            {
                Body = "Internal note",
                IsInternal = true
            });

        var userView = await service.GetMyTicketAsync(userId, ticket.Id);

        Assert.True(result.Success);
        Assert.Equal(TicketStatus.WaitingForSupport, ticket.Status);
        Assert.NotNull(userView.Data);
        Assert.DoesNotContain(userView.Data.Messages, message => message.IsInternal);
        Assert.DoesNotContain(userView.Data.Messages, message => message.Body == "Internal note");
    }

    [Fact]
    public async Task AddUserMessageAsync_WhenTicketIsClosed_ReturnsConflict()
    {
        var userId = Guid.NewGuid();
        var ticket = CreateTicket(userId);
        ticket.Close(Guid.NewGuid(), DateTime.UtcNow);
        var repository = new FakeTicketRepository
        {
            ExistingUserIds = { userId },
            Tickets = { ticket }
        };
        var service = CreateService(repository, new FakeTicketNumberGenerator("TKT-2"));

        var result = await service.AddUserMessageAsync(
            userId,
            ticket.Id,
            new CreateTicketMessageRequest { Body = "Can you reopen?" });

        Assert.False(result.Success);
        Assert.Equal(409, result.StatusCode);
    }

    [Fact]
    public async Task CloseTicketAsync_ClosesTicket()
    {
        var userId = Guid.NewGuid();
        var ticket = CreateTicket(userId);
        var adminId = Guid.NewGuid();
        var auditLogWriter = new FakeAuditLogWriter();
        var repository = new FakeTicketRepository
        {
            ExistingUserIds = { userId },
            Tickets = { ticket }
        };
        var service = CreateService(
            repository,
            new FakeTicketNumberGenerator("TKT-2"),
            auditLogWriter: auditLogWriter);

        var result = await service.CloseTicketAsync(adminId, ticket.Id);

        Assert.True(result.Success);
        Assert.Equal(TicketStatus.Closed, ticket.Status);
        Assert.NotNull(ticket.ClosedAt);
        var auditEntry = Assert.Single(auditLogWriter.Entries);
        Assert.Equal(AuditActionType.TicketClosed, auditEntry.ActionType);
        Assert.Equal(nameof(Ticket), auditEntry.EntityName);
        Assert.Equal(ticket.Id, auditEntry.EntityId);
        Assert.DoesNotContain("Initial body", auditEntry.Description ?? string.Empty);
    }

    [Fact]
    public async Task CreateTicketAsync_GeneratesServerSideUniqueTicketNumber()
    {
        var userId = Guid.NewGuid();
        var repository = new FakeTicketRepository
        {
            ExistingUserIds = { userId },
            ExistingTicketNumbers = { "TKT-DUPLICATE" }
        };
        var service = CreateService(
            repository,
            new FakeTicketNumberGenerator("TKT-DUPLICATE", "TKT-UNIQUE"));

        var result = await service.CreateTicketAsync(
            userId,
            new CreateTicketRequest
            {
                Subject = "Number generation",
                Body = "Create a ticket."
            });

        Assert.True(result.Success);
        Assert.Equal("TKT-UNIQUE", result.Data?.TicketNumber);
        Assert.Equal(2, repository.TicketNumberChecks);
    }

    private static TicketService CreateService(
        FakeTicketRepository repository,
        ITicketNumberGenerator ticketNumberGenerator,
        FakeNotificationService? notificationService = null,
        FakeAuditLogWriter? auditLogWriter = null)
    {
        return new TicketService(
            repository,
            ticketNumberGenerator,
            notificationService ?? new FakeNotificationService(),
            auditLogWriter ?? new FakeAuditLogWriter());
    }

    private static Ticket CreateTicket(Guid userId)
    {
        return Ticket.Create(
            userId,
            "TKT-EXISTING",
            "Existing ticket",
            "Initial body",
            null,
            null,
            DateTime.UtcNow);
    }

    private sealed class FakeTicketNumberGenerator : ITicketNumberGenerator
    {
        private readonly Queue<string> _numbers;

        public FakeTicketNumberGenerator(params string[] numbers)
        {
            _numbers = new Queue<string>(numbers);
        }

        public string Generate(DateTime createdAtUtc)
        {
            return _numbers.Count == 0 ? "TKT-FALLBACK" : _numbers.Dequeue();
        }
    }

    private sealed class FakeTicketRepository : ITicketRepository
    {
        public HashSet<Guid> ExistingUserIds { get; } = new();
        public HashSet<string> ExistingTicketNumbers { get; } = new();
        public Dictionary<Guid, Guid> OrderOwners { get; } = new();
        public List<Ticket> Tickets { get; } = new();
        public int TicketNumberChecks { get; private set; }

        public Task<bool> UserExistsAsync(Guid userId)
        {
            return Task.FromResult(ExistingUserIds.Contains(userId));
        }

        public Task<bool> OrderBelongsToUserAsync(Guid userId, Guid orderId)
        {
            return Task.FromResult(
                OrderOwners.TryGetValue(orderId, out var ownerId) &&
                ownerId == userId);
        }

        public Task<bool> TicketNumberExistsAsync(string ticketNumber)
        {
            TicketNumberChecks++;
            return Task.FromResult(ExistingTicketNumbers.Contains(ticketNumber));
        }

        public Task<PagedResultDto<TicketListItemDto>> GetMyTicketsAsync(
            Guid userId,
            int page,
            int pageSize,
            TicketStatus? status)
        {
            var items = Tickets
                .Where(ticket =>
                    ticket.UserId == userId &&
                    (!status.HasValue || ticket.Status == status.Value))
                .Select(ToListItem)
                .ToList();

            return Task.FromResult(new PagedResultDto<TicketListItemDto>
            {
                Items = items,
                Page = page,
                PageSize = pageSize,
                TotalCount = items.Count
            });
        }

        public Task<PagedResultDto<TicketListItemDto>> GetAdminTicketsAsync(
            int page,
            int pageSize,
            TicketStatus? status,
            Guid? userId,
            Guid? orderId,
            string? search)
        {
            var items = Tickets
                .Where(ticket =>
                    (!status.HasValue || ticket.Status == status.Value) &&
                    (!userId.HasValue || ticket.UserId == userId.Value) &&
                    (!orderId.HasValue || ticket.OrderId == orderId.Value))
                .Select(ToListItem)
                .ToList();

            return Task.FromResult(new PagedResultDto<TicketListItemDto>
            {
                Items = items,
                Page = page,
                PageSize = pageSize,
                TotalCount = items.Count
            });
        }

        public Task<TicketDetailDto?> GetTicketDetailAsync(
            Guid ticketId,
            Guid? userId,
            bool includeInternalMessages)
        {
            var ticket = Tickets.FirstOrDefault(value =>
                value.Id == ticketId &&
                (!userId.HasValue || value.UserId == userId.Value));
            if (ticket == null)
            {
                return Task.FromResult<TicketDetailDto?>(null);
            }

            var detail = new TicketDetailDto
            {
                TicketId = ticket.Id,
                TicketNumber = ticket.TicketNumber,
                UserId = ticket.UserId,
                Subject = ticket.Subject,
                Status = ticket.Status.ToString(),
                Priority = ticket.Priority.ToString(),
                CreatedAt = ticket.CreatedAt,
                LastActivityAt = ticket.LastActivityAt,
                FirstResponseAt = ticket.FirstResponseAt,
                ClosedAt = ticket.ClosedAt,
                CustomerName = "Customer",
                CustomerEmail = "customer@example.com",
                Messages = ticket.Messages
                    .Where(message => includeInternalMessages || !message.IsInternal)
                    .OrderBy(message => message.CreatedAt)
                    .Select(message => new TicketMessageDto
                    {
                        Id = message.Id,
                        AuthorId = message.AuthorId,
                        AuthorName = message.AuthorId == ticket.UserId
                            ? "Customer"
                            : "Support",
                        AuthorRole = message.AuthorId == ticket.UserId
                            ? nameof(UserRole.User)
                            : nameof(UserRole.Admin),
                        Body = message.Body,
                        IsInternal = message.IsInternal,
                        CreatedAt = message.CreatedAt
                    })
                    .ToList()
            };

            return Task.FromResult<TicketDetailDto?>(detail);
        }

        public Task<Ticket?> GetTicketForUpdateAsync(Guid ticketId, Guid? userId)
        {
            return Task.FromResult(Tickets.FirstOrDefault(ticket =>
                ticket.Id == ticketId &&
                (!userId.HasValue || ticket.UserId == userId.Value)));
        }

        public Task AddTicketAsync(Ticket ticket)
        {
            Tickets.Add(ticket);
            ExistingTicketNumbers.Add(ticket.TicketNumber);
            return Task.CompletedTask;
        }

        public Task<bool> SaveChangesAsync()
        {
            return Task.FromResult(true);
        }

        private static TicketListItemDto ToListItem(Ticket ticket)
        {
            return new TicketListItemDto
            {
                TicketId = ticket.Id,
                TicketNumber = ticket.TicketNumber,
                Subject = ticket.Subject,
                Status = ticket.Status.ToString(),
                Priority = ticket.Priority.ToString(),
                CreatedAt = ticket.CreatedAt,
                LastActivityAt = ticket.LastActivityAt,
                CustomerName = "Customer",
                CustomerEmail = "customer@example.com"
            };
        }
    }
}
