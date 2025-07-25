using System.Text.Json.Serialization;

namespace Taiju.Scenes.Model.Events;

public class Trigger : Event {
  [JsonPropertyName("Type")]
  public string Type { get; set; }
}
