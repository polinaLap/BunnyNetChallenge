using System.Threading.Channels;

namespace BunnyNetChallenge.RequestProcessors
{
    public abstract class BaseRequestProcessor<T>: IRequestProcessor<T>
    {
        private readonly Channel<T> _requestChannel;

        public BaseRequestProcessor(Channel<T> requestChannel)
        {
            _requestChannel = requestChannel;
        }

        public async Task StartProcessingAsync(CancellationToken cancellationToken)
        {
            while (await _requestChannel.Reader.WaitToReadAsync(cancellationToken))
            {
                while (_requestChannel.Reader.TryRead(out var request))
                {
                    await ProcessRequestAsync(request);
                }
            }
        }

        protected abstract Task ProcessRequestAsync(T request);
    }
}
