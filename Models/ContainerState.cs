namespace BunnyNetChallenge.Models
{
    public enum ContainerState
    {
        QueuedToStart,
        QueuedToStop,
        ImagePulled,
        ImagePullingError,
        ContainerCreationError,
        ContainerStartingError,
        ContainerStoppingError,
        Created,
        Running,
        Restarting,
        Paused,
        Exited,
        Dead
    }
}
