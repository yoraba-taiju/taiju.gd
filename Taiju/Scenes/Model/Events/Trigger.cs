using System.Text.Json.Serialization;

namespace Taiju.Scenes.Model.Events;

public class Trigger : Event {
  [JsonPropertyName("Id")]
  public string Id { get; set; }
}
