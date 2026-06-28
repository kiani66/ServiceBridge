using ServiceBridge.Application.Common;
using ServiceBridge.Shared;

namespace ServiceBridge.Tests;

public class FoundationTests
{
    [Fact]
    public void ServiceResult_success_contains_value()
    {
        var result = ServiceResult<string>.Success(ServiceBridgeConstants.ProductName);

        Assert.True(result.IsSuccess);
        Assert.Equal("ServiceBridge", result.Value);
        Assert.Empty(result.Errors);
    }
}
