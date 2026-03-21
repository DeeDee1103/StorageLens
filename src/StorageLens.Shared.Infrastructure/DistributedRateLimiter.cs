using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Threading.RateLimiting;

namespace StorageLens.Shared.Infrastructure;

/// <summary>
/// Distributed rate-limiting policy backed by <see cref="IDistributedCache"/>.
///
/// In Docker / production: wire IDistributedCache to Redis via
///   builder.Services.AddStackExchangeRedisCache(o => o.Configuration = redisConn)
/// In development (no Redis): the default in-memory IDistributedCache is used
/// automatically — no code change needed.
///
/// Each service registers this policy by calling
/// <see cref="DistributedRateLimiterExtensions.AddStorageLensRateLimiting"/>.
///
/// The fixed-window algorithm:
///   - Window: 1 minute
///   - Limit: configurable per service (default: 1 000 req/window)
///   - Key: per-IP (X-Forwarded-For → RemoteIpAddress fallback)
///   - Rejected requests: 429 + Retry-After header
/// </summary>
public sealed class DistributedFixedWindowPolicy : IRateLimiterPolicy<string>
{
    private readonly IDistributedCache _cache;
    private readonly ILogger<DistributedFixedWindowPolicy> _logger;
    private readonly int _permitLimit;
    private static readonly TimeSpan Window = TimeSpan.FromMinutes(1);

    public DistributedFixedWindowPolicy(
        IDistributedCache cache,
        ILogger<DistributedFixedWindowPolicy> logger,
        int permitLimit = 1_000)
    {
        _cache  = cache;
        _logger = logger;
        _permitLimit = permitLimit;
    }

    public Func<OnRejectedContext, CancellationToken, ValueTask>? OnRejected =>
        static (ctx, _) =>
        {
            ctx.HttpContext.Response.Headers.RetryAfter =
                ((int)Window.TotalSeconds).ToString();
            return ValueTask.CompletedTask;
        };

    public RateLimitPartition<string> GetPartition(HttpContext httpContext)
    {
        var clientKey = GetClientKey(httpContext);

        return RateLimitPartition.Get<string>(clientKey, key =>
            new DistributedWindowLimiter(_cache, _logger, key, _permitLimit, Window));
    }

    private static string GetClientKey(HttpContext ctx)
    {
        if (ctx.Request.Headers.TryGetValue("X-Forwarded-For", out var fwd))
            return $"rl:{fwd.ToString().Split(',')[0].Trim()}";

        return $"rl:{ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown"}";
    }
}

/// <summary>
/// A <see cref="RateLimiter"/> that uses <see cref="IDistributedCache"/> to
/// share window counts across multiple service replicas.
/// </summary>
internal sealed class DistributedWindowLimiter : RateLimiter
{
    private readonly IDistributedCache _cache;
    private readonly ILogger _logger;
    private readonly string _cacheKey;
    private readonly int _limit;
    private readonly TimeSpan _window;

    public DistributedWindowLimiter(
        IDistributedCache cache,
        ILogger logger,
        string cacheKey,
        int limit,
        TimeSpan window)
    {
        _cache    = cache;
        _logger   = logger;
        _cacheKey = cacheKey;
        _limit    = limit;
        _window   = window;
    }

    public override TimeSpan? IdleDuration => null;

    public override RateLimiterStatistics? GetStatistics() => null;

    protected override async ValueTask<RateLimitLease> AcquireAsyncCore(
        int permitCount,
        CancellationToken cancellationToken)
    {
        try
        {
            var current = await IncrementAsync(cancellationToken);
            if (current <= _limit)
                return new GrantedLease();

            _logger.LogWarning("Rate limit exceeded for key {CacheKey}", _cacheKey);
            return new DeniedLease();
        }
        catch (Exception ex)
        {
            // On cache failure (Redis unavailable) fall back to allow — prefer availability
            // over strict rate limiting to avoid a cascading outage.
            _logger.LogError(ex, "Distributed rate limiter cache error for {CacheKey} — allowing request", _cacheKey);
            return new GrantedLease();
        }
    }

    protected override RateLimitLease AttemptAcquireCore(int permitCount)
        => new DeniedLease(); // Synchronous path not supported; use async

    private async Task<long> IncrementAsync(CancellationToken ct)
    {
        var data = await _cache.GetAsync(_cacheKey, ct);
        long count = data is null ? 0 : BitConverter.ToInt64(data);

        count++;

        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = _window
        };
        await _cache.SetAsync(_cacheKey, BitConverter.GetBytes(count), options, ct);
        return count;
    }

    private sealed class GrantedLease : RateLimitLease
    {
        public override bool IsAcquired => true;
        public override IEnumerable<string> MetadataNames => [];
        public override bool TryGetMetadata(string metadataName, out object? metadata)
        {
            metadata = null;
            return false;
        }
        protected override void Dispose(bool disposing) { }
    }

    private sealed class DeniedLease : RateLimitLease
    {
        public override bool IsAcquired => false;
        public override IEnumerable<string> MetadataNames => [MetadataName.RetryAfter];
        public override bool TryGetMetadata(string metadataName, out object? metadata)
        {
            if (metadataName == MetadataName.RetryAfter)
            {
                metadata = TimeSpan.FromMinutes(1);
                return true;
            }
            metadata = null;
            return false;
        }
        protected override void Dispose(bool disposing) { }
    }
}

/// <summary>
/// Extension methods to configure distributed rate limiting for a StorageLens service.
/// </summary>
public static class DistributedRateLimiterExtensions
{
    /// <summary>
    /// Replaces the per-service in-memory AddRateLimiter() with a distributed policy
    /// backed by IDistributedCache (Redis in production, in-memory in development).
    ///
    /// Call this instead of AddRateLimiter() in each service's Program.cs to enable
    /// cross-replica rate limiting.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="redisConnectionString">
    ///   Optional Redis connection string.  When provided, registers StackExchange.Redis
    ///   as the IDistributedCache backing store.  When null/empty, the default in-memory
    ///   cache is used (safe for single-instance development).
    /// </param>
    /// <param name="permitLimit">Maximum requests per minute per IP (default: 1 000).</param>
    public static IServiceCollection AddStorageLensRateLimiting(
        this IServiceCollection services,
        string? redisConnectionString,
        int permitLimit = 1_000)
    {
        if (!string.IsNullOrWhiteSpace(redisConnectionString))
        {
            services.AddStackExchangeRedisCache(o =>
                o.Configuration = redisConnectionString);
        }
        else
        {
            // Development fallback — in-memory IDistributedCache
            services.AddDistributedMemoryCache();
        }

        services.AddSingleton(sp =>
            new DistributedFixedWindowPolicy(
                sp.GetRequiredService<IDistributedCache>(),
                sp.GetRequiredService<ILogger<DistributedFixedWindowPolicy>>(),
                permitLimit));

        services.AddRateLimiter(options =>
        {
            options.AddPolicy<string, DistributedFixedWindowPolicy>("distributed-global");
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        });

        return services;
    }
}
