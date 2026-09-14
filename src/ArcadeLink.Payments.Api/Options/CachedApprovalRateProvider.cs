using System.Globalization;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;

namespace ArcadeLink.Payments.Api.Options;

public class CachedApprovalRateProvider(IDistributedCache cache, IOptions<PaymentsOptions> options)
    : IApprovalRateProvider
{
    private const string CacheKey = "config:approval-rate";
    private static readonly DistributedCacheEntryOptions CacheEntryOptions = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(30)
    };

    public async Task<double> GetAsync()
    {
        var cached = await cache.GetStringAsync(CacheKey);
        if (cached is not null && double.TryParse(cached, CultureInfo.InvariantCulture, out var cachedValue))
            return cachedValue;

        var rate = options.Value.ApprovalRate;
        await cache.SetStringAsync(CacheKey, rate.ToString(CultureInfo.InvariantCulture), CacheEntryOptions);
        return rate;
    }
}
