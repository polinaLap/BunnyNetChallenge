using BunnyNetChallenge.ContainerStateCache;
using BunnyNetChallenge.Models;
using Docker.DotNet;
using Docker.DotNet.Models;
using System.Threading.Channels;
using ContainerState = BunnyNetChallenge.Models.ContainerState;

namespace BunnyNetChallenge.RequestProcessors
{
    public class CreateContainerRequestProcessor : BaseRequestProcessor<CreateContainerRequest>
    {
        private readonly IDockerClient _dockerClient;
        private readonly IContainersStateCache _containersStateCache;

        public CreateContainerRequestProcessor(
            Channel<CreateContainerRequest> requestChannel, 
            IDockerClient dockerClient,
            IContainersStateCache containersStateCache,
            ILogger<CreateContainerRequestProcessor> logger) : base(requestChannel, logger)
        {
            _dockerClient = dockerClient;
            _containersStateCache = containersStateCache;
        }

        protected override async Task ProcessRequestAsync(CreateContainerRequest request, CancellationToken stoppingToken)
        {
            await PullImageAsync(request, stoppingToken);
            var containerId = await CreateContainerAsync(request, stoppingToken);
            await StartContainerAsync(containerId, request, stoppingToken);

            _logger.LogDebug("Container {0} by image {1} has started", request.ContainerName, request.ImageName);
        }

        private async Task PullImageAsync(CreateContainerRequest request, CancellationToken stoppingToken)
        {
            var containerState = new ContainerStateModel { Name = request.ContainerName };
            try
            {
                var paremeters = new ImagesCreateParameters
                {
                    FromImage = request.ImageName,
                    Tag = string.IsNullOrEmpty(request.ImageTag) ? "latest" : request.ImageTag,
                };
                await _dockerClient.Images.CreateImageAsync(
                    paremeters,
                    authConfig: null,
                    new Progress<JSONMessage>(m => _logger.LogDebug(m.Status)),
                    stoppingToken);                
                containerState.State = ContainerState.ImagePulled;
                _containersStateCache.AddOrUpdate(containerState);
            }
            catch
            {
                containerState.State = ContainerState.ImagePullingError;
                _containersStateCache.AddOrUpdate(containerState);
                throw;
            }
        }

        private async Task<string> CreateContainerAsync(CreateContainerRequest request, CancellationToken stoppingToken)
        {
            var containerState = new ContainerStateModel { Name = request.ContainerName };
            try
            {
                var parameters = new CreateContainerParameters()
                {
                    Image = request.ImageName,
                    Name = request.ContainerName
                };
                var response = await _dockerClient.Containers.CreateContainerAsync(parameters, stoppingToken);

                containerState.Id = response.ID;
                containerState.State = ContainerState.Created;
                _containersStateCache.AddOrUpdate(containerState);

                return response.ID;
            }
            catch
            {
                containerState.State = ContainerState.ContainerCreationError;
                _containersStateCache.AddOrUpdate(containerState);
                throw;
            }
        }

        private async Task StartContainerAsync(string containerId, CreateContainerRequest request, CancellationToken stoppingToken)
        {
            var containerState = new ContainerStateModel { Name = request.ContainerName, Id = containerId };
            try
            {
                var isStarted = await _dockerClient.Containers.StartContainerAsync(
                    containerId,
                    new ContainerStartParameters(),
                    stoppingToken);

                containerState.State = isStarted ? ContainerState.Running : ContainerState.ContainerStartingError;
                _containersStateCache.AddOrUpdate(containerState);
            }
            catch
            {
                containerState.State = ContainerState.ContainerStartingError;
                _containersStateCache.AddOrUpdate(containerState);
                throw;
            }
        }
    }
}
