using System.Threading.Channels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ApexCommerce.Application.Common.Interfaces;

namespace ApexCommerce.Infrastructure.BackgroundJobs;

public sealed class BackgroundTaskQueue : IBackgroundTaskQueue
{
    private readonly Channel<Func<IServiceProvider, CancellationToken, ValueTask>> _queue;

    public BackgroundTaskQueue(int capacity = 1000)
    {
        var options = new BoundedChannelOptions(capacity)
        {
            FullMode = BoundedChannelFullMode.Wait
        };
        _queue = Channel.CreateBounded<Func<IServiceProvider, CancellationToken, ValueTask>>(options);
    }

    public async ValueTask QueueBackgroundWorkItemAsync(Func<IServiceProvider, CancellationToken, ValueTask> workItem)
    {
        ArgumentNullException.ThrowIfNull(workItem);
        await _queue.Writer.WriteAsync(workItem);
    }

    public async ValueTask<Func<IServiceProvider, CancellationToken, ValueTask>> DequeueAsync(CancellationToken cancellationToken)
    {
        return await _queue.Reader.ReadAsync(cancellationToken);
    }
}

public sealed class QueuedHostedService : BackgroundService
{
    private readonly IBackgroundTaskQueue _taskQueue;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<QueuedHostedService> _logger;

    public QueuedHostedService(
        IBackgroundTaskQueue taskQueue,
        IServiceProvider serviceProvider,
        ILogger<QueuedHostedService> logger)
    {
        _taskQueue = taskQueue;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Background Task Queue Worker started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var workItem = await _taskQueue.DequeueAsync(stoppingToken);

                using var scope = _serviceProvider.CreateScope();
                await workItem(scope.ServiceProvider, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred executing queued background work item.");
            }
        }

        _logger.LogInformation("Background Task Queue Worker stopped.");
    }
}

