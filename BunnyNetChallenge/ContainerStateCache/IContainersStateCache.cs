using BunnyNetChallenge.Models;

namespace BunnyNetChallenge.ContainerStateCache
{
    public interface IContainersStateCache
    {
        int Count { get; }
        void AddOrUpdate(ContainerStateModel containerState);
        ContainerStateModel? Get(string containerName);
        IEnumerable<ContainerStateModel> GetPaginatedList(int page, int pageSize);
    }
}