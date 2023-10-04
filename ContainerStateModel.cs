using System.Text.Json.Serialization;

namespace BunnyNetChallenge
{
    public class ContainerStateModel
    {
        public string ContainerId { get; set; }

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public ContainerState State { get; set; }

        public ContainerStateModel(string containerId, ContainerState state)
        {
            ContainerId = containerId;
            State = state;
        }
    }
}
