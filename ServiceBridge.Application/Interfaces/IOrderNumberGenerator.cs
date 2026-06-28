namespace ServiceBridge.Application.Interfaces;

public interface IOrderNumberGenerator
{
    string Generate(DateTime utcNow);
}
