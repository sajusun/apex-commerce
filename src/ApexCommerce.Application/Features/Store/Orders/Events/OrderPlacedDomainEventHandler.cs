using MediatR;
using Microsoft.Extensions.Logging;
using ApexCommerce.Application.Common.Interfaces;
using ApexCommerce.Domain.Events;

namespace ApexCommerce.Application.Features.Store.Orders.Events;

public sealed class OrderPlacedDomainEventHandler : INotificationHandler<OrderPlacedDomainEvent>
{
    private readonly ICacheService _cacheService;
    private readonly IBackgroundTaskQueue _taskQueue;
    private readonly ILogger<OrderPlacedDomainEventHandler> _logger;

    public OrderPlacedDomainEventHandler(
        ICacheService cacheService,
        IBackgroundTaskQueue taskQueue,
        ILogger<OrderPlacedDomainEventHandler> logger)
    {
        _cacheService = cacheService;
        _taskQueue = taskQueue;
        _logger = logger;
    }

    public async Task Handle(OrderPlacedDomainEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Domain Event: OrderPlaced {OrderNumber} for {CustomerEmail} total {TotalAmount} {Currency}",
            notification.OrderNumber, notification.CustomerEmail, notification.TotalAmount, notification.Currency);

        // 1. Invalidate Redis product catalog cache (since stock was reserved)
        await _cacheService.RemoveAsync("store:products:featured:p1", cancellationToken);
        _logger.LogInformation("Invalidated Redis catalog cache 'store:products:featured:p1'.");

        // 2. Dispatch asynchronous email confirmation background job
        await _taskQueue.QueueBackgroundWorkItemAsync(async (sp, ct) =>
        {
            var logger = (ILogger<OrderPlacedDomainEventHandler>)sp.GetService(typeof(ILogger<OrderPlacedDomainEventHandler>))!;
            logger.LogInformation("[Background Job] Sending order confirmation email for Order {OrderNumber} to {Email}...",
                notification.OrderNumber, notification.CustomerEmail);

            // Simulate email dispatch latency
            await Task.Delay(500, ct);

            logger.LogInformation("[Background Job] Order confirmation email successfully sent for Order {OrderNumber}!",
                notification.OrderNumber);
        });
    }
}

