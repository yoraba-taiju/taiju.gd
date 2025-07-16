using System.Text.Json.Serialization;

namespace Taiju.Scenes.Model.Events;

public class Spawn : Event {
  [JsonPropertyName("Path")]
  public string Path { get; set; }
}
