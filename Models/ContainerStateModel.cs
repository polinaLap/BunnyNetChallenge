using System.Text.Json.Serialization;

namespace BunnyNetChallenge.Models
{
    public class ContainerStateModel
    {
        public string ContainerName { get; set; }

        public string ContainerId { get; set; }

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public ContainerState State { get; set; }
    }
}
