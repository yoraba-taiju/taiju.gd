using System.Text.Json.Serialization;

namespace Taiju.Scenes.Model.Events;

public class Trigger : Event {
  [JsonPropertyName("TriggerType")]
  public string TriggerType { get; set; }
}
