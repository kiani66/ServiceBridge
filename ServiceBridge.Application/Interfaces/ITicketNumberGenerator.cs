namespace ServiceBridge.Application.Interfaces;

public interface ITicketNumberGenerator
{
    string Generate(DateTime createdAtUtc);
}
