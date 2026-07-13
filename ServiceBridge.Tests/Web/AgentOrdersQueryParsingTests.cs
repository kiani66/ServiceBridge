using System.Reflection;
using ServiceBridge.Domain.Enums;
using Xunit;

namespace ServiceBridge.Tests.Web;

public class AgentOrdersQueryParsingTests
{
    [Theory]
    [InlineData("Paid", OrderStatus.Paid)]
    [InlineData("paid", OrderStatus.Paid)]
    public void ParseStatus_ValidName_ReturnsDefinedStatus(
        string value,
        OrderStatus expected)
    {
        Assert.Equal(expected, ParseStatus(value));
    }

    [Theory]
    [InlineData("invalid")]
    [InlineData("999")]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void ParseStatus_InvalidOrMissingValue_ReturnsNull(string? value)
    {
        Assert.Null(ParseStatus(value));
    }

    [Theory]
    [InlineData("2", 2)]
    [InlineData("abc", 1)]
    [InlineData("0", 1)]
    [InlineData("-1", 1)]
    [InlineData("999999999999999999999999999999999999", 1)]
    [InlineData("", 1)]
    [InlineData(null, 1)]
    public void ParsePage_NormalizesQueryValue(string? value, int expected)
    {
        Assert.Equal(expected, ParsePage(value));
    }

    private static OrderStatus? ParseStatus(string? value)
    {
        return (OrderStatus?)ParserMethod("ParseStatus").Invoke(null, [value]);
    }

    private static int ParsePage(string? value)
    {
        return (int)ParserMethod("ParsePage").Invoke(null, [value])!;
    }

    private static MethodInfo ParserMethod(string name)
    {
        return typeof(ServiceBridge.Web.Components.Pages.AgentOrders).GetMethod(
            name,
            BindingFlags.Static | BindingFlags.NonPublic)!;
    }
}
