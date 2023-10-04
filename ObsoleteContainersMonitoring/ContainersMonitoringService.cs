using Docker.DotNet;
using Docker.DotNet.Models;

namespace BunnyNetChallenge.ObsoleteContainersMonitoring
{
    [Obsolete("Done before task clarification")]
    public class ContainersMonitoringService : IContainersMonitoringService
    {
        private readonly IContainersStateCache _containerStatesCache;
        private readonly IDockerClient _dockerClient;
        private static readonly Dictionary<string, ContainerState>
            _eventToContainerStateMap = new Dictionary<string, ContainerState>
        {
            { "create", ContainerState.Created },
            { "die", ContainerState.Dead },
            { "kill", ContainerState.Dead },
            { "oom", ContainerState.Dead },
            { "pause", ContainerState.Paused },
            { "restart", ContainerState.Restarting },
            { "start", ContainerState.Running },
            { "stop", ContainerState.Exited },
            { "unpause", ContainerState.Running }
        };

        private static readonly Dictionary<string, ContainerState>
            _stringToContainerStateMap = new Dictionary<string, ContainerState>
        {
            { "created", ContainerState.Created },
            { "exited", ContainerState.Exited },
            { "dead", ContainerState.Dead },
            { "paused", ContainerState.Paused },
            { "restarting", ContainerState.Restarting },
            { "running", ContainerState.Running }
        };

        public ContainersMonitoringService(IContainersStateCache containerStatesCache, IDockerClient dockerClient)
        {
            _containerStatesCache = containerStatesCache;
            _dockerClient = dockerClient;
        }

        public async Task StartMonitoringAsync(CancellationToken cancellationToken)
        {
            await LoadExistingContainers();
            var progress = new Progress<Message>(ProcessEvent);

            var filter = new ContainerEventsParameters
            {
                Filters = new Dictionary<string, IDictionary<string, bool>>
            {
                { "type", new Dictionary<string, bool> { { "container", true } } }
            }
            };
            await _dockerClient.System.MonitorEventsAsync(filter, progress, cancellationToken);
        }

        private async Task LoadExistingContainers()
        {
            var containers = await _dockerClient.Containers.ListContainersAsync(
                new ContainersListParameters()
                {
                    All = true,
                });

            foreach (var container in containers)
            {
                var containerId = container.ID;
                var containerState = container.State;
                if (!string.IsNullOrWhiteSpace(container.State)
                    && _stringToContainerStateMap.TryGetValue(containerState, out var state))
                {
                    _containerStatesCache.Add(new ContainerStateModel(containerId, state));
                }
            }
        }

        private void ProcessEvent(Message dockerEvent)
        {
            var containerId = dockerEvent.Actor.ID;
            var containerState = dockerEvent.Status;
            if (!string.IsNullOrWhiteSpace(containerState)
                    && _eventToContainerStateMap.TryGetValue(containerState, out var state))
            {
                _containerStatesCache.Add(new ContainerStateModel(containerId, state));
            }
        }
    }
}
