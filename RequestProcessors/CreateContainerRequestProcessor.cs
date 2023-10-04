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

        protected override async Task ProcessRequestAsync(CreateContainerRequest request)
        {
            var containerState = new ContainerStateModel()
            {
                ContainerName = request.ContainerName
            };
            await PullImageAsync(request, containerState);
            await CreateContainerAsync(request, containerState);
            await StartContainerAsync(containerState);

            _logger.LogDebug("Container {containerName} by image {image} has started", request.ContainerName, request.ImageName);
        }

        private async Task PullImageAsync(CreateContainerRequest request, ContainerStateModel containerState)
        {
            try
            {
                await _dockerClient.Images.CreateImageAsync(
                    new ImagesCreateParameters
                    {
                        FromImage = request.ImageName,
                        Tag = string.IsNullOrEmpty(request.ImageTag) ? "latest" : request.ImageTag,
                    },
                    authConfig: null,
                    new Progress<JSONMessage>(m => _logger.LogDebug(m.Status)));

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

        private async Task CreateContainerAsync(CreateContainerRequest request, ContainerStateModel containerState)
        {
            try
            {
                var response = await _dockerClient.Containers.CreateContainerAsync(new CreateContainerParameters()
                {
                    Image = request.ImageName,
                    Name = request.ContainerName
                });
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

        private async Task StartContainerAsync(ContainerStateModel containerState)
        {
            try
            {
                var isStarted = await _dockerClient.Containers.StartContainerAsync(
                    containerState.ContainerId,
                    new ContainerStartParameters());

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
