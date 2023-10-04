namespace BunnyNetChallenge.RequestProcessors
{
    public interface IRequestProcessor<T>
    {
        Task StartProcessingAsync(CancellationToken cancellationToken);
    }
}
