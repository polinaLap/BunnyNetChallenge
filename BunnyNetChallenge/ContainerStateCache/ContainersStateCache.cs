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
            _containerStates[containerState.Name] = containerState;
        }

        public ContainerStateModel? Get(string containerName)
        {
            var exists = _containerStates.TryGetValue(containerName, out ContainerStateModel containerState);
            return exists ? containerState : null;
        }

        public IEnumerable<ContainerStateModel> GetPaginatedList(int pageSize, int page)
        {
            var startIndex = (page - 1) * pageSize;
            return _containerStates.Values.OrderBy(x => x.Name).Skip(startIndex).Take(pageSize);
        }
    }
}
