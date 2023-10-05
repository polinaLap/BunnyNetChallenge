namespace BunnyNetChallenge.Models
{
    public class PaginatedListResponse
    {
        public IEnumerable<ContainerStateModel> Containers { get; set; }
        public int TotalCount { get; set; }
    }
}
