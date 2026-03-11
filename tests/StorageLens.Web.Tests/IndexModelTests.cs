using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging.Abstractions;
using StorageLens.Shared.Contracts;
using StorageLens.Web.Clients;
using StorageLens.Web.Pages;
using Xunit;

namespace StorageLens.Web.Tests;

public class IndexModelTests
{
    [Fact]
    public async Task OnGetAsync_WhenAnalyticsReturnsData_PopulatesDashboard()
    {
        var expected = new DashboardDto(
            new DashboardKpiDto(100_000_000L, 5000, 10, 50_000_000L),
            [new ChartPointDto("Jan", 1.2)],
            [new ChartPointDto(".pdf", 300)],
            [new ChartPointDto("NAS01", 2.5)],
            [],
            [],
            []);

        var handler = new StubHttpMessageHandler((req, ct) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(expected)
            }));

        var client = new HttpClient(handler) { BaseAddress = new Uri("http://analytics") };
        var analyticsClient = new AnalyticsApiClient(client, NullLogger<AnalyticsApiClient>.Instance);
        var model = new IndexModel(analyticsClient, NullLogger<IndexModel>.Instance);
        SetupPageContext(model);

        await model.OnGetAsync(CancellationToken.None);

        Assert.False(model.ServiceUnavailable);
        Assert.Equal(100_000_000L, model.Dashboard.Kpis.TotalStorageBytes);
        Assert.Equal(5000, model.Dashboard.Kpis.TotalFiles);
        Assert.Single(model.Dashboard.StorageGrowth);
    }

    [Fact]
    public async Task OnGetAsync_WhenAnalyticsUnavailable_SetsServiceUnavailable()
    {
        var handler = new StubHttpMessageHandler((req, ct) =>
            Task.FromException<HttpResponseMessage>(new HttpRequestException("Connection refused")));

        var client = new HttpClient(handler) { BaseAddress = new Uri("http://analytics") };
        var analyticsClient = new AnalyticsApiClient(client, NullLogger<AnalyticsApiClient>.Instance);
        var model = new IndexModel(analyticsClient, NullLogger<IndexModel>.Instance);
        SetupPageContext(model);

        await model.OnGetAsync(CancellationToken.None);

        Assert.True(model.ServiceUnavailable);
        // Dashboard should still have safe default values
        Assert.Equal(0, model.Dashboard.Kpis.TotalFiles);
    }

    [Fact]
    public async Task OnGetAsync_WhenAnalyticsReturnsNull_SetsServiceUnavailable()
    {
        var handler = new StubHttpMessageHandler((req, ct) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("null")
                {
                    Headers = { ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json") }
                }
            }));

        var client = new HttpClient(handler) { BaseAddress = new Uri("http://analytics") };
        var analyticsClient = new AnalyticsApiClient(client, NullLogger<AnalyticsApiClient>.Instance);
        var model = new IndexModel(analyticsClient, NullLogger<IndexModel>.Instance);
        SetupPageContext(model);

        await model.OnGetAsync(CancellationToken.None);

        Assert.True(model.ServiceUnavailable);
    }

    internal static void SetupPageContext(PageModel model)
    {
        var httpContext = new DefaultHttpContext();
        var routeData = new RouteData();
        var pageActionDescriptor = new PageActionDescriptor();
        var modelState = new ModelStateDictionary();
        var actionContext = new ActionContext(httpContext, routeData, pageActionDescriptor, modelState);
        var metadataProvider = new EmptyModelMetadataProvider();
        var pageContext = new PageContext(actionContext)
        {
            ViewData = new Microsoft.AspNetCore.Mvc.ViewFeatures.ViewDataDictionary(metadataProvider, modelState)
        };
        model.PageContext = pageContext;
    }

    private sealed class StubHttpMessageHandler(
        Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => handler(request, cancellationToken);
    }
}