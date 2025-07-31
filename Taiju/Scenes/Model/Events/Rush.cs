#nullable enable
using System.Text.Json.Serialization;

namespace Taiju.Scenes.Model.Events;

public class Rush : Event {
  [JsonPropertyName("Spawns")]
  public required Spawn[] Spawns { get; init; }
}
