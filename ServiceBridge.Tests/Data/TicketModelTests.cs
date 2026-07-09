using Microsoft.EntityFrameworkCore;
using ServiceBridge.Domain.Entities;
using ServiceBridge.Infrastructure.Data;
using Xunit;

namespace ServiceBridge.Tests.Data;

public class TicketModelTests
{
    private readonly ServiceBridgeDbContext _context;

    public TicketModelTests()
    {
        var options = new DbContextOptionsBuilder<ServiceBridgeDbContext>()
            .UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=ServiceBridgeTicketModelTests")
            .Options;

        _context = new ServiceBridgeDbContext(options);
    }

    [Fact]
    public void TicketMessage_CannotBeModified()
    {
        var message = new TicketMessage
        {
            Id = Guid.NewGuid(),
            TicketId = Guid.NewGuid(),
            AuthorId = Guid.NewGuid(),
            Body = "Initial message",
            CreatedAt = DateTime.UtcNow
        };

        _context.TicketMessages.Attach(message);
        message.Body = "Edited message";

        var exception = Assert.Throws<InvalidOperationException>(
            () => _context.SaveChanges());

        Assert.Equal("Ticket messages are append-only.", exception.Message);
    }

    [Fact]
    public void TicketMessage_CannotBeDeleted()
    {
        var message = new TicketMessage
        {
            Id = Guid.NewGuid(),
            TicketId = Guid.NewGuid(),
            AuthorId = Guid.NewGuid(),
            Body = "Initial message",
            CreatedAt = DateTime.UtcNow
        };

        _context.TicketMessages.Attach(message);
        _context.TicketMessages.Remove(message);

        var exception = Assert.Throws<InvalidOperationException>(
            () => _context.SaveChanges());

        Assert.Equal("Ticket messages are append-only.", exception.Message);
    }
}
