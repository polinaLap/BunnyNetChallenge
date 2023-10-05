using BunnyNetChallenge.ContainerStateCache;
using BunnyNetChallenge.Models;

namespace BunnyNetChallengeTests
{
    public class ContainersStateCacheTests
    {
        [Fact]
        public void AddOrUpdate_ShouldAddOrUpdateContainerState()
        {
            // Arrange
            var cache = new ContainersStateCache();
            var containerState = new ContainerStateModel { Name = "Container1", State = ContainerState.Created };

            // Act
            cache.AddOrUpdate(containerState);

            // Assert
            var retrievedContainerState = cache.Get("Container1");
            Assert.Equal(containerState, retrievedContainerState);
        }

        [Fact]
        public void AddOrUpdate_ModelChangeShouldNotAffectCache()
        {
            // Arrange
            var cache = new ContainersStateCache();
            var containerState = new ContainerStateModel { Name = "Container1", State = ContainerState.Created };
            cache.AddOrUpdate(containerState);

            // Act
            containerState.State = ContainerState.Running;

            // Assert
            var retrievedContainerState = cache.Get("Container1");
            Assert.NotEqual(containerState, retrievedContainerState);
            Assert.Equal(ContainerState.Created, retrievedContainerState?.State);
        }

        [Fact]
        public void AddOrUpdate_ShouldUpdateExistedContainerState()
        {
            // Arrange
            var cache = new ContainersStateCache();
            var containerState = new ContainerStateModel { Name = "Container1", State = ContainerState.Created };
            cache.AddOrUpdate(containerState);

            // Act
            containerState.State = ContainerState.Running;
            cache.AddOrUpdate(containerState);

            // Assert
            var retrievedContainerState = cache.Get("Container1");
            Assert.Equal(containerState, retrievedContainerState);
        }

        [Fact]
        public void AddOrUpdate_ShouldBeThreadSafe()
        {
            // Arrange
            var cache = new ContainersStateCache();
            const int numThreads = 10;
            const int iterationsPerThread = 100;

            // Act
            var tasks = new List<Task>();
            for (var i = 0; i < numThreads; i++)
            {
                var threadNum = i;
                tasks.Add(Task.Run(() =>
                {
                    for (var j = 0; j < iterationsPerThread; j++)
                    {
                        var containerName = $"Container{threadNum * iterationsPerThread + j}";
                        var containerState = new ContainerStateModel { Name = containerName, State = ContainerState.Running };
                        cache.AddOrUpdate(containerState);
                    }
                }));
            }

            Task.WaitAll(tasks.ToArray());

            // Assert
            var expectedCount = numThreads * iterationsPerThread;
            var actualCount = cache.GetPaginatedList(expectedCount, 1).Count();
            Assert.Equal(expectedCount, actualCount);
        }

        [Fact]
        public async Task AddOrUpdate_ToSameKey_ShouldBeThreadSafe()
        {
            // Arrange
            var cache = new ContainersStateCache();
            const int numThreads = 10;

            var containerState = new ContainerStateModel { Name = "Container1", State = ContainerState.Created };
            async Task UpdateContainerStateAsync()
            {
                for (var i = 0; i < 10; i++)
                {
                    // Simulate some work being done
                    await Task.Delay(10);

                    containerState = new ContainerStateModel { Name = "Container1" };
                    containerState.State = ContainerState.Running;

                    cache.AddOrUpdate(containerState);
                }
            }

            // Act
            var tasks = new Task[numThreads];
            for (var i = 0; i < numThreads; i++)
            {
                tasks[i] = Task.Run(UpdateContainerStateAsync);
            }

            await Task.WhenAll(tasks);

            // Assert
            var finalContainerState = cache.Get("Container1");
            Assert.Equal(ContainerState.Running, finalContainerState?.State);
        }


        [Fact]
        public void Get_ShouldReturnCorrectContainerState()
        {
            // Arrange
            var cache = new ContainersStateCache();
            var containerState = new ContainerStateModel { Name = "Container2", State = ContainerState.Running };
            cache.AddOrUpdate(containerState);

            // Act
            var retrievedContainerState = cache.Get("Container2");

            // Assert
            Assert.Equal(containerState, retrievedContainerState);
        }

        [Fact]
        public void GetPaginatedList_Should_ReturnCorrectPageSizeAndPage()
        {
            // Arrange
            var cache = new ContainersStateCache();
            var containerStates = new[]
            {
            new ContainerStateModel { Name = "Container3", State = ContainerState.Created },
            new ContainerStateModel { Name = "Container4", State = ContainerState.Running },
            new ContainerStateModel { Name = "Container5", State = ContainerState.Exited }
        };
            foreach (var containerState in containerStates)
            {
                cache.AddOrUpdate(containerState);
            }

            // Act
            var paginatedList = cache.GetPaginatedList(pageSize: 2, page: 2).ToList();

            // Assert
            Assert.Single(paginatedList);
            Assert.Equal("Container5", paginatedList[0].Name);
            Assert.Equal(ContainerState.Exited, paginatedList[0].State);
        }
    }
}