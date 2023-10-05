using System.ComponentModel;
using System.Threading.Channels;
using BunnyNetChallenge.ContainerStateCache;
using BunnyNetChallenge.Models;
using BunnyNetChallenge.RequestProcessors;
using Docker.DotNet;
using Docker.DotNet.Models;
using Microsoft.Extensions.Logging;
using Moq;
using ContainerState = BunnyNetChallenge.Models.ContainerState;

namespace BunnyNetChallengeTests
{
    public class CreateContainerRequestProcessorTests
    {
        private readonly Mock<IDockerClient> _dockerClientMock = new();
        private readonly Mock<IContainersStateCache> _containersStateCacheMock = new();
        private readonly Channel<CreateContainerRequest> _channel = Channel.CreateUnbounded<CreateContainerRequest>();

        private readonly CreateContainerRequestProcessor processor;

        private static readonly CreateContainerRequest _request = new CreateContainerRequest
        {
            ContainerName = "TestContainer",
            ImageName = "TestImage",
            ImageTag = "latest"
        };

        public CreateContainerRequestProcessorTests()
        {
            processor = new CreateContainerRequestProcessor(
                _channel,
                _dockerClientMock.Object,
                _containersStateCacheMock.Object,
                Mock.Of<ILogger<CreateContainerRequestProcessor>>());
        }

        [Fact]
        public async Task ProcessRequestAsync_SuccessfulContainerCreationAndStart_ShouldUpdateContainerState()
        {
            // Arrange
            var containerId = "ContainerId123";
            _channel.Writer.TryWrite(_request);
            _dockerClientMock.Setup(client => client.Images.CreateImageAsync(
                It.IsAny<ImagesCreateParameters>(),
                It.IsAny<AuthConfig>(),
                It.IsAny<IProgress<JSONMessage>>(),
                It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            _dockerClientMock.Setup(client => client.Containers.CreateContainerAsync(
                It.IsAny<CreateContainerParameters>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(new CreateContainerResponse { ID = containerId });

            _dockerClientMock.Setup(client => client.Containers.StartContainerAsync(
                containerId,
                It.IsAny<ContainerStartParameters>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            // Act
            await processor.StartAsync(default);

            // Assert
            _containersStateCacheMock.Verify(
                cache => cache.AddOrUpdate(It.Is<ContainerStateModel>(cs => cs.State == ContainerState.ImagePulled)),
                Times.Once);
            _containersStateCacheMock.Verify(
                cache => cache.AddOrUpdate(It.Is<ContainerStateModel>(cs => cs.State == ContainerState.Created && cs.Id == containerId)),
                Times.Once);
            _containersStateCacheMock.Verify(
                cache => cache.AddOrUpdate(It.Is<ContainerStateModel>(cs => cs.State == ContainerState.Running && cs.Id == containerId)),
                Times.Once);
        }

        [Fact]
        public async Task ProcessRequestAsync_ImagePullFailure_ShouldUpdateContainerStateWithError()
        {
            // Arrange
            _channel.Writer.TryWrite(_request);
            _dockerClientMock.Setup(client => client.Images.CreateImageAsync(
                It.IsAny<ImagesCreateParameters>(),
                It.IsAny<AuthConfig>(),
                It.IsAny<IProgress<JSONMessage>>(),
                It.IsAny<CancellationToken>()))
                .Throws(new DockerApiException(System.Net.HttpStatusCode.BadRequest, "Some error"));

            // Act
            await processor.StartAsync(default);

            // Assert
            _containersStateCacheMock.Verify(
                cache => cache.AddOrUpdate(It.Is<ContainerStateModel>(cs => cs.State == ContainerState.ImagePullingError)),
                Times.Once);
        }

        [Fact]
        public async Task ProcessRequestAsync_ContainerCreationFailure_ShouldUpdateContainerStateWithError()
        {
            // Arrange
            _channel.Writer.TryWrite(_request);
            _dockerClientMock.Setup(client => client.Images.CreateImageAsync(
                It.IsAny<ImagesCreateParameters>(),
                It.IsAny<AuthConfig>(),
                It.IsAny<IProgress<JSONMessage>>(),
                It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            _dockerClientMock.Setup(client => client.Containers.CreateContainerAsync(
                It.IsAny<CreateContainerParameters>(),
                It.IsAny<CancellationToken>()))
                .Throws(new DockerApiException(System.Net.HttpStatusCode.BadRequest, "Some error"));

            // Act
            await processor.StartAsync(default);

            // Assert
            _containersStateCacheMock.Verify(
                cache => cache.AddOrUpdate(It.Is<ContainerStateModel>(cs => cs.State == ContainerState.ContainerCreationError)),
                Times.Once);
        }

        [Fact]
        public async Task ProcessRequestAsync_ContainerStartFailure_ShouldUpdateContainerStateWithError()
        {
            // Arrange
            var containerId = "ContainerId123";
            _channel.Writer.TryWrite(_request);
            _dockerClientMock.Setup(client => client.Images.CreateImageAsync(
                It.IsAny<ImagesCreateParameters>(),
                It.IsAny<AuthConfig>(),
                It.IsAny<IProgress<JSONMessage>>(),
                It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            _dockerClientMock.Setup(client => client.Containers.CreateContainerAsync(
                It.IsAny<CreateContainerParameters>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(new CreateContainerResponse { ID = containerId });

            _dockerClientMock.Setup(client => client.Containers.StartContainerAsync(
                containerId,
                It.IsAny<ContainerStartParameters>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(false); 

            // Act
            await processor.StartAsync(default);

            // Assert
            _containersStateCacheMock.Verify(
                cache => cache.AddOrUpdate(It.Is<ContainerStateModel>(cs => cs.State == ContainerState.ContainerStartingError && cs.Id == containerId)),
                Times.Once);
        }
    }
}
