using System.Collections.Concurrent;
using BunnyNetChallenge.Models;

namespace BunnyNetChallenge.ContainerStateCache
{
    public class ContainersStateCache : IContainersStateCache
    {
        private readonly ConcurrentDictionary<string, ContainerStateModel> _containerStates;

        public ContainersStateCache()
        {
            _containerStates = new ConcurrentDictionary<string, ContainerStateModel>();
        }

        public void AddOrUpdate(ContainerStateModel containerState)
        {
            _containerStates[containerState.ContainerName] = containerState;
        }

        public ContainerStateModel Get(string containerName)
        {
            _containerStates.TryGetValue(containerName, out var containerState);
            return containerState;
        }

        public IEnumerable<ContainerStateModel> GetPaginatedList(int pageSize, int page)
        {
            var startIndex = (page - 1) * pageSize;
            return _containerStates.Values.Skip(startIndex).Take(pageSize);
        }
    }
}
