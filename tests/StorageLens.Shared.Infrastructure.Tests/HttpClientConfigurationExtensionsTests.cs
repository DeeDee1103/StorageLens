using StorageLens.Shared.Infrastructure;
using Xunit;

namespace StorageLens.Shared.Infrastructure.Tests;

public class HttpClientConfigurationExtensionsTests
{
    [Theory]
    [InlineData("GET", true)]
    [InlineData("HEAD", true)]
    [InlineData("OPTIONS", true)]
    [InlineData("POST", false)]
    [InlineData("PUT", false)]
    [InlineData("PATCH", false)]
    [InlineData("DELETE", false)]
    public void ShouldApplyRetryPolicy_ReturnsExpectedValue(string method, bool expected)
    {
        var result = HttpClientConfigurationExtensions.ShouldApplyRetryPolicy(new HttpMethod(method));

        Assert.Equal(expected, result);
    }
}
