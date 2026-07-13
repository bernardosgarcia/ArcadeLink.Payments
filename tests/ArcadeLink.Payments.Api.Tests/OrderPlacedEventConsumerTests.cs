using ArcadeLink.Contracts;
using ArcadeLink.Payments.Api.Consumers;
using ArcadeLink.Payments.Api.Options;
using MassTransit;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace ArcadeLink.Payments.Api.Tests;

public class OrderPlacedEventConsumerTests
{
    private static async Task<(ServiceProvider Provider, ITestHarness Harness)> StartHarnessAsync(double approvalRate)
    {
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.SetMinimumLevel(LogLevel.Warning));
        services.Configure<PaymentsOptions>(options => options.ApprovalRate = approvalRate);

        services.AddMassTransitTestHarness(x =>
        {
            x.AddConsumer<OrderPlacedEventConsumer>();
        });

        var provider = services.BuildServiceProvider(true);
        var harness = provider.GetRequiredService<ITestHarness>();
        await harness.Start();

        return (provider, harness);
    }

    [Fact]
    public async Task ApprovalRate_1_always_publishes_Approved()
    {
        var (provider, harness) = await StartHarnessAsync(approvalRate: 1);
        await using var _ = provider;

        var orderId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var gameId = Guid.NewGuid();

        await harness.Bus.Publish(new OrderPlacedEvent(orderId, userId, gameId, 29.90m));

        Assert.True(await harness.Consumed.Any<OrderPlacedEvent>());
        Assert.True(await harness.Published.Any<PaymentProcessedEvent>(
            x => x.Context.Message.OrderId == orderId && x.Context.Message.Status == "Approved"));
    }

    [Fact]
    public async Task ApprovalRate_0_always_publishes_Rejected()
    {
        var (provider, harness) = await StartHarnessAsync(approvalRate: 0);
        await using var _ = provider;

        var orderId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var gameId = Guid.NewGuid();

        await harness.Bus.Publish(new OrderPlacedEvent(orderId, userId, gameId, 29.90m));

        Assert.True(await harness.Consumed.Any<OrderPlacedEvent>());
        Assert.True(await harness.Published.Any<PaymentProcessedEvent>(
            x => x.Context.Message.OrderId == orderId && x.Context.Message.Status == "Rejected"));
    }
}
