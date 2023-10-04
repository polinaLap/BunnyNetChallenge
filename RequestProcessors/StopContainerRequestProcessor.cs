using BunnyNetChallenge.ContainerStateCache;
using Docker.DotNet;
using Docker.DotNet.Models;
using System.Threading.Channels;
using ContainerState = BunnyNetChallenge.Models.ContainerState;

namespace BunnyNetChallenge.RequestProcessors
{
    public class StopContainerRequestProcessor : BaseRequestProcessor<StopContainerRequest>
    {
        private readonly IDockerClient _dockerClient;
        private readonly IContainersStateCache _containersStateCache;

        public StopContainerRequestProcessor(
            Channel<StopContainerRequest> requestChannel,
            IDockerClient dockerClient,
            IContainersStateCache containersStateCache,
            ILogger<StopContainerRequestProcessor> logger) 
            : base(requestChannel, logger)
        {
            _dockerClient = dockerClient;
            _containersStateCache = containersStateCache;  
        }

        protected override async Task ProcessRequestAsync(StopContainerRequest request, CancellationToken stoppingToken)
        {
            var containerState = _containersStateCache.Get(request.ContainerName);
            if (containerState == null)
            {
                return;
            }
            try
            {               
                await _dockerClient.Containers.StopContainerAsync(
                    request.ContainerName, 
                    new ContainerStopParameters(), 
                    stoppingToken);
                containerState.State = ContainerState.Exited;
                _containersStateCache.AddOrUpdate(containerState);

                _logger.LogDebug("Container {0} by image {1} has stopped", request.ContainerName);
            }
            catch
            {
                containerState.State = ContainerState.ContainerStoppingError;
                _containersStateCache.AddOrUpdate(containerState);
                throw;
            }
        }
    }
}
