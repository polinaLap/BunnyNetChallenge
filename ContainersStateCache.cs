using System.Collections.Concurrent;

namespace BunnyNetChallenge
{
    public class ContainersStateCache : IContainersStateCache
    {
        private readonly ConcurrentDictionary<string, ContainerStateModel> _containerStates;

        public ContainersStateCache()
        {
            _containerStates = new ConcurrentDictionary<string, ContainerStateModel>();
        }

        public void Add(ContainerStateModel containerState)
        {
            _containerStates[containerState.ContainerId] = containerState;
        }

        public ContainerStateModel Get(string id)
        {
            _containerStates.TryGetValue(id, out var containerState);
            return containerState;
        }

        public IEnumerable<ContainerStateModel> GetPaginatedList(int pageSize, int page)
        {
            var startIndex = (page - 1) * pageSize;
            return _containerStates.Values.Skip(startIndex).Take(pageSize);
        }
    }
}
