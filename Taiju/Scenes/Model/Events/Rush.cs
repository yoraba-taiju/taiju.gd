using System.Text.Json.Serialization;

namespace Taiju.Scenes.Model.Events;

public class Rush : Event {
  [JsonPropertyName("Spawns")]
  public Spawn[] Spawns { get; set; }

}
