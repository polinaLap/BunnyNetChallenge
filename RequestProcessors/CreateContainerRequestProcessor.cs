using Docker.DotNet;
using Docker.DotNet.Models;
using System.Threading.Channels;

namespace BunnyNetChallenge.RequestProcessors
{
    public class CreateContainerRequestProcessor : BaseRequestProcessor<CreateContainerRequest>
    {

        private readonly IDockerClient _dockerClient;
        private readonly ILogger<CreateContainerRequestProcessor> _logger;
        public CreateContainerRequestProcessor(Channel<CreateContainerRequest> requestChannel, IDockerClient dockerClient, ILogger<CreateContainerRequestProcessor> logger) : base(requestChannel)
        {
            _dockerClient = dockerClient;
            _logger = logger;
        }

        protected override async Task ProcessRequestAsync(CreateContainerRequest request)
        {
            //pull image if not pulled yet

            Console.WriteLine("Thread {0} is in processor", Thread.CurrentThread.ManagedThreadId);
            await _dockerClient.Images.CreateImageAsync(
                new ImagesCreateParameters
                {
                    FromImage = request.ImageName,
                    Tag = string.IsNullOrEmpty(request.Tag) ? "latest" : request.Tag,
                },
                null,
                new Progress<JSONMessage>(m => _logger.LogDebug(m.Status)));

            //create container using pulled image
            var response = await _dockerClient.Containers.CreateContainerAsync(new CreateContainerParameters()
            {
                Image = request.ImageName
            });

            //start created container
            await _dockerClient.Containers.StartContainerAsync(
                response.ID,
                new ContainerStartParameters());

            _logger.LogDebug("Container {containerId} by image {image} has started", response.ID, request.ImageName);
        }
    }
}
