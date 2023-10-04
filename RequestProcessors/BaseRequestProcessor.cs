using System.Threading.Channels;

namespace BunnyNetChallenge.RequestProcessors
{
    public abstract class BaseRequestProcessor<T> : BackgroundService
    {
        private readonly Channel<T> _requestChannel;
        protected readonly ILogger<BaseRequestProcessor<T>> _logger;

        public BaseRequestProcessor(Channel<T> requestChannel, ILogger<BaseRequestProcessor<T>> logger)
        {
            _requestChannel = requestChannel;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation(
                $"Queued Hosted Service is running.{Environment.NewLine}" +
                $"{Environment.NewLine}Tap W to add a work item to the " +
                $"background queue.{Environment.NewLine}");

            await BackgroundProcessing(stoppingToken);
        }

        private async Task BackgroundProcessing(CancellationToken stoppingToken)
        {
            while (await _requestChannel.Reader.WaitToReadAsync(stoppingToken))
            {
                while (_requestChannel.Reader.TryRead(out var request))
                {
                    try
                    {
                        await ProcessRequestAsync(request);
                    }
                    catch (Exception ex)
                    {
                        // here I want to keep the process running despite any error
                        // but in reality I'd think about analyzing errors deeper
                        // and adding retries/alerts for cases when the problem is about http connection
                        _logger.LogError(ex.Message);
                    }
                }
            }
        }

        protected abstract Task ProcessRequestAsync(T request);
    }
}
