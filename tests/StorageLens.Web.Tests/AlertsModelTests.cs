using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using StorageLens.Shared.Contracts;
using StorageLens.Web.Clients;
using StorageLens.Web.Pages;
using StorageLens.Web.Services;
using Xunit;

namespace StorageLens.Web.Tests;

public class AlertsModelTests
{
    [Fact]
    public async Task OnGetAsync_WhenUsageMeetsMaxThreshold_SendsAlertEmail()
    {
        var dashboard = new DashboardDto(
            new DashboardKpiDto(1024, 123, 95, 100),
            [],
            [],
            [],
            [],
            [],
            []);

        var handler = new StubHttpMessageHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(dashboard)
            }));

        var client = new HttpClient(handler) { BaseAddress = new Uri("http://analytics") };
        var analyticsClient = new AnalyticsApiClient(client, NullLogger<AnalyticsApiClient>.Instance);
        var alertService = new StubAlertEmailService();

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Alerts:MinThresholdPercent"] = "70",
                ["Alerts:MaxThresholdPercent"] = "90",
                ["Alerts:RecipientEmail"] = "dddevacct16783@gmail.com"
            })
            .Build();

        var env = new StubHostEnvironment();
        var tierAccess = new AllowAllTierAccessService();

        var model = new AlertsModel(config, env, analyticsClient, alertService, tierAccess);
        SetupPageContext(model);

        await model.OnGetAsync(CancellationToken.None);

        Assert.Equal(95, model.CurrentUsagePercent);
        Assert.Equal(1, alertService.SendCount);
    }

    private static void SetupPageContext(PageModel model)
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers.Cookie =
            $"{EnvironmentSettingsModel.EditEnvCookieName}={EnvironmentSettingsModel.Development}";

        var routeData = new RouteData();
        var pageActionDescriptor = new Microsoft.AspNetCore.Mvc.RazorPages.CompiledPageActionDescriptor();
        var modelState = new ModelStateDictionary();
        var actionContext = new Microsoft.AspNetCore.Mvc.ActionContext(httpContext, routeData, pageActionDescriptor, modelState);
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

    private sealed class StubAlertEmailService : IAlertEmailService
    {
        public int SendCount { get; private set; }

        public Task<AlertDispatchResult> SendThresholdReachedEmailAsync(
            int currentPercent,
            int thresholdPercent,
            string recipientEmail,
            CancellationToken cancellationToken = default)
        {
            SendCount++;
            return Task.FromResult(new AlertDispatchResult(true, "sent"));
        }
    }

    private sealed class StubHostEnvironment : IWebHostEnvironment
    {
        public string ApplicationName { get; set; } = "StorageLens.Web";
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
        public string WebRootPath { get; set; } = string.Empty;
        public string EnvironmentName { get; set; } = Environments.Development;
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }

    private sealed class AllowAllTierAccessService : ITierAccessService
    {
        public PricingTier? ActiveTier => null;
        public IReadOnlyList<PricingTier> AllTiers => [];

        public bool IsServiceAvailable(string serviceId) => true;
    }
}
