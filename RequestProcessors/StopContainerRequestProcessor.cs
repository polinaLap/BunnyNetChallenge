using Docker.DotNet;
using Docker.DotNet.Models;
using System.Threading.Channels;

namespace BunnyNetChallenge.RequestProcessors
{
    public class StopContainerRequestProcessor : BaseRequestProcessor<StopContainerRequest>
    {

        private readonly IDockerClient _dockerClient;
        private readonly ILogger<StopContainerRequestProcessor> _logger;
        public StopContainerRequestProcessor(Channel<StopContainerRequest> requestChannel, IDockerClient dockerClient, ILogger<StopContainerRequestProcessor> logger) 
            : base(requestChannel)
        {
            _dockerClient = dockerClient;
            _logger = logger;
        }

        protected override async Task ProcessRequestAsync(StopContainerRequest request)
        {
            await _dockerClient.Containers.StopContainerAsync(request.ContainerID, new ContainerStopParameters(), CancellationToken.None);
            _logger.LogDebug("Container {containerId} by image {image} has stopped", request.ContainerID);
        }
    }
}
