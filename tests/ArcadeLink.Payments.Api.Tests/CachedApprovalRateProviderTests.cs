using ArcadeLink.Payments.Api.Options;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Xunit;

namespace ArcadeLink.Payments.Api.Tests;

public class CachedApprovalRateProviderTests
{
    [Fact]
    public async Task Returns_and_caches_the_configured_value_on_first_call()
    {
        var cache = new MemoryDistributedCache(Microsoft.Extensions.Options.Options.Create(new MemoryDistributedCacheOptions()));
        var options = Microsoft.Extensions.Options.Options.Create(new PaymentsOptions { ApprovalRate = 0.42 });
        var provider = new CachedApprovalRateProvider(cache, options);

        var rate = await provider.GetAsync();

        Assert.Equal(0.42, rate);
    }

    [Fact]
    public async Task Second_call_returns_the_cached_value_even_if_the_underlying_option_changed()
    {
        var cache = new MemoryDistributedCache(Microsoft.Extensions.Options.Options.Create(new MemoryDistributedCacheOptions()));
        var options = Microsoft.Extensions.Options.Options.Create(new PaymentsOptions { ApprovalRate = 0.1 });
        var provider = new CachedApprovalRateProvider(cache, options);

        await provider.GetAsync();
        options.Value.ApprovalRate = 0.9;

        var rate = await provider.GetAsync();

        Assert.Equal(0.1, rate);
    }
}
