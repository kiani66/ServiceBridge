using ServiceBridge.Application.Interfaces;

namespace ServiceBridge.Tests.TestDoubles;

internal class FakeOrderNumberGenerator : IOrderNumberGenerator
{
    private readonly Queue<string> _numbers;

    public FakeOrderNumberGenerator(params string[] numbers)
    {
        _numbers = new Queue<string>(numbers);
    }

    public string Generate(DateTime utcNow)
    {
        return _numbers.Count > 0 ? _numbers.Dequeue() : "PB-DEFAULT";
    }
}
