using System.Text.Json.Serialization;

namespace BunnyNetChallenge.Models
{
    public struct ContainerStateModel
    {
        public string Name { get; set; }

        public string Id { get; set; }

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public ContainerState State { get; set; }
    }
}
