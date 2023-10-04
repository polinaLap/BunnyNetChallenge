using BunnyNetChallenge.Models;

namespace BunnyNetChallenge.ContainerStateCache
{
    public interface IContainersStateCache
    {
        void AddOrUpdate(ContainerStateModel containerState);
        ContainerStateModel Get(string containerName);
        IEnumerable<ContainerStateModel> GetPaginatedList(int page, int pageSize);
    }
}