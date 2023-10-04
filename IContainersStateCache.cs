namespace BunnyNetChallenge
{
    public interface IContainersStateCache
    {
        void Add(ContainerStateModel containerState);
        ContainerStateModel Get(string id);
        IEnumerable<ContainerStateModel> GetPaginatedList(int page, int pageSize);
    }
}