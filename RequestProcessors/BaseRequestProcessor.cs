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
            while (await _requestChannel.Reader.WaitToReadAsync(stoppingToken))
            {
                while (_requestChannel.Reader.TryRead(out var request))
                {
                    try
                    {
                        await ProcessRequestAsync(request, stoppingToken);
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

        protected abstract Task ProcessRequestAsync(T request, CancellationToken stoppingToken);
    }
}
