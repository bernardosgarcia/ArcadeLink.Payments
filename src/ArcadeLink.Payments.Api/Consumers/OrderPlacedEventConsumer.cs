using ArcadeLink.Contracts;
using ArcadeLink.Payments.Api.Options;
using MassTransit;
using Microsoft.Extensions.Options;

namespace ArcadeLink.Payments.Api.Consumers;

public class OrderPlacedEventConsumer(ILogger<OrderPlacedEventConsumer> logger, IOptions<PaymentsOptions> options)
    : IConsumer<OrderPlacedEvent>
{
    public async Task Consume(ConsumeContext<OrderPlacedEvent> context)
    {
        var message = context.Message;
        var approved = Random.Shared.NextDouble() < options.Value.ApprovalRate;
        var status = approved ? "Approved" : "Rejected";

        logger.LogInformation(
            "Pedido {OrderId} (usuário {UserId}, jogo {GameId}, preço {Price}) processado: {Status}",
            message.OrderId, message.UserId, message.GameId, message.Price, status);

        await context.Publish(new PaymentProcessedEvent(message.OrderId, message.UserId, message.GameId, status));
    }
}
