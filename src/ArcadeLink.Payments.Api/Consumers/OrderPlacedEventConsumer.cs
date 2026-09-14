using ArcadeLink.Contracts;
using ArcadeLink.Payments.Api.Options;
using MassTransit;

namespace ArcadeLink.Payments.Api.Consumers;

public class OrderPlacedEventConsumer(ILogger<OrderPlacedEventConsumer> logger, IApprovalRateProvider approvalRateProvider)
    : IConsumer<OrderPlacedEvent>
{
    public async Task Consume(ConsumeContext<OrderPlacedEvent> context)
    {
        var message = context.Message;
        var approvalRate = await approvalRateProvider.GetAsync();
        var approved = Random.Shared.NextDouble() < approvalRate;
        var status = approved ? "Approved" : "Rejected";

        logger.LogInformation(
            "Pedido {OrderId} (usuário {UserId}, jogo {GameId}, preço {Price}) processado: {Status}",
            message.OrderId, message.UserId, message.GameId, message.Price, status);

        await context.Publish(new PaymentProcessedEvent(message.OrderId, message.UserId, message.GameId, status));
    }
}
