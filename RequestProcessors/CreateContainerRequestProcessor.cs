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
            var containerState = new ContainerStateModel { ContainerName = request.ContainerName };
            await PullImageAsync(request, containerState, stoppingToken);
            await CreateContainerAsync(request, containerState, stoppingToken);
            await StartContainerAsync(containerState, stoppingToken);

            _logger.LogDebug("Container {0} by image {1} has started", request.ContainerName, request.ImageName);
        }

        private async Task PullImageAsync(CreateContainerRequest request, ContainerStateModel containerState, CancellationToken stoppingToken)
        {
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

        private async Task CreateContainerAsync(CreateContainerRequest request, ContainerStateModel containerState, CancellationToken stoppingToken)
        {
            try
            {
                var parameters = new CreateContainerParameters()
                {
                    Image = request.ImageName,
                    Name = request.ContainerName
                };
                var response = await _dockerClient.Containers.CreateContainerAsync(parameters, stoppingToken);

                containerState.ContainerId = response.ID;
                containerState.State = ContainerState.Created;
                _containersStateCache.AddOrUpdate(containerState);
            }
            catch
            {
                containerState.State = ContainerState.ContainerCreationError;
                _containersStateCache.AddOrUpdate(containerState);
                throw;
            }
        }

        private async Task StartContainerAsync(ContainerStateModel containerState, CancellationToken stoppingToken)
        {
            try
            {
                var isStarted = await _dockerClient.Containers.StartContainerAsync(
                    containerState.ContainerId,
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
