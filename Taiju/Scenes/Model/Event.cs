using System.Text.Json.Serialization;

namespace Taiju.Scenes.Model;

public class Event {
  [JsonPropertyName("X")]
  public float X { get; set; }
  [JsonPropertyName("Y")]
  public float Y { get; set; }
}
