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
    public class StopContainerRequestProcessorTests
    {
        private readonly Mock<IDockerClient> _dockerClientMock = new();
        private readonly Mock<IContainersStateCache> _containersStateCacheMock = new();
        private readonly Channel<StopContainerRequest> _channel = Channel.CreateUnbounded<StopContainerRequest>();

        private readonly StopContainerRequestProcessor processor;

        public StopContainerRequestProcessorTests()
        {
            processor = new StopContainerRequestProcessor(
                _channel,
                _dockerClientMock.Object,
                _containersStateCacheMock.Object,
                Mock.Of<ILogger<StopContainerRequestProcessor>>());
        }

        [Fact]
        public async Task ProcessRequestAsync_ContainerStateNotFound_ShouldNotStopContainer()
        {
            // Arrange
            var request = new StopContainerRequest { ContainerName = "NonExistentContainer" };
            _containersStateCacheMock.Setup(cache => cache.Get(request.ContainerName)).Returns((ContainerStateModel?)null);
            _channel.Writer.TryWrite(request);

            // Act
            await processor.StartAsync(default);

            // Assert
            _dockerClientMock.Verify(
                client => client.Containers.StopContainerAsync(
                    It.IsAny<string>(),
                    It.IsAny<ContainerStopParameters>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task ProcessRequestAsync_ContainerStoppingError_ShouldUpdateContainerState()
        {
            // Arrange
            var request = new StopContainerRequest { ContainerName = "TestContainer" };
            _dockerClientMock.Setup(client => client.Containers.StopContainerAsync(
                request.ContainerName,
                It.IsAny<ContainerStopParameters>(),
                It.IsAny<CancellationToken>()))
                .Throws(new DockerApiException(System.Net.HttpStatusCode.BadRequest, "Some error"));

            var containerState = new ContainerStateModel { Name = request.ContainerName, State = ContainerState.Running };
            _containersStateCacheMock.Setup(cache => cache.Get(request.ContainerName)).Returns(containerState);
            _channel.Writer.TryWrite(request);

            // Act
            await processor.StartAsync(default);

            // Assert
            _containersStateCacheMock.Verify(
                cache => cache.AddOrUpdate(It.Is<ContainerStateModel>(cs => cs.State == ContainerState.ContainerStoppingError)),
                Times.Once);
        }

        [Fact]
        public async Task ProcessRequestAsync_SuccessfulContainerStop_ShouldUpdateContainerState()
        {
            // Arrange
            var request = new StopContainerRequest { ContainerName = "TestContainer" };
            _dockerClientMock.Setup(client => client.Containers.StopContainerAsync(
                request.ContainerName,
                It.IsAny<ContainerStopParameters>(),
                It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(true));

            var containerState = new ContainerStateModel { Name = request.ContainerName, State = ContainerState.Running };
            _containersStateCacheMock.Setup(cache => cache.Get(request.ContainerName)).Returns(containerState);
            _channel.Writer.TryWrite(request);

            // Act
            await processor.StartAsync(default);

            // Assert
            _containersStateCacheMock.Verify(
                cache => cache.AddOrUpdate(It.Is<ContainerStateModel>(cs => cs.State == ContainerState.Exited)),
                Times.Once);
        }
    }
}
