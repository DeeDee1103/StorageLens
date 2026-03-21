using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace StorageLens.Shared.Infrastructure;

public static class BackgroundDatabaseInitialization
{
    public static void Start<TDbContext>(
        WebApplication app,
        string serviceName,
        Func<IServiceProvider, TDbContext, CancellationToken, Task>? seedAsync = null)
        where TDbContext : DbContext
    {
        app.Lifetime.ApplicationStarted.Register(() =>
        {
            _ = Task.Run(async () =>
            {
                using var scope = app.Services.CreateScope();
                var loggerFactory = scope.ServiceProvider.GetRequiredService<ILoggerFactory>();
                var startupLogger = loggerFactory.CreateLogger("Startup.Database");
                var serviceLogger = loggerFactory.CreateLogger(serviceName);

                try
                {
                    var context = scope.ServiceProvider.GetRequiredService<TDbContext>();
                    await StartupDatabaseInitializer.EnsureCreatedWithRetryAsync(
                        context,
                        startupLogger,
                        cancellationToken: app.Lifetime.ApplicationStopping);

                    if (seedAsync is not null)
                    {
                        await seedAsync(scope.ServiceProvider, context, app.Lifetime.ApplicationStopping);
                    }

                    serviceLogger.LogInformation("Background database initialization completed.");
                }
                catch (OperationCanceledException) when (app.Lifetime.ApplicationStopping.IsCancellationRequested)
                {
                    serviceLogger.LogInformation("Background database initialization canceled during shutdown.");
                }
                catch (Exception ex)
                {
                    serviceLogger.LogError(ex, "Background database initialization failed.");
                }
            });
        });
    }
}