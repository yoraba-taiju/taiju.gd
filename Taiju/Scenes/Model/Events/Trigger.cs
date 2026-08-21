using System.Text.Json.Serialization;

namespace Taiju.Scenes.Model.Events;

public class Trigger : Event {
  [JsonPropertyName("Id")]
  public required string Id { get; init; }
}
