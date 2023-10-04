namespace BunnyNetChallenge.ObsoleteContainersMonitoring
{
    [Obsolete("Done before task clarification")]
    public interface IContainersMonitoringService
    {
        Task StartMonitoringAsync(CancellationToken cancellationToken);
    }
}