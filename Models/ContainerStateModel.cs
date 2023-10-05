using System.Text.Json.Serialization;

namespace BunnyNetChallenge.Models
{
    public struct ContainerStateModel
    {
        public string Name { get; set; }

        public string ContainerId { get; set; }

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public ContainerState State { get; set; }
    }
}
