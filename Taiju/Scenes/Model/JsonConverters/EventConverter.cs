using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Taiju.Scenes.Model.JsonConverters;

public class EventConverter : JsonConverter<Model.Event> {
  public override Model.Event Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
    using var jsonDoc = JsonDocument.ParseValue(ref reader);
    var root = jsonDoc.RootElement;

    if (!root.TryGetProperty("Type", out var typeProp) || typeProp.ValueKind != JsonValueKind.String) {
      throw new JsonException("Type property is missing or invalid.");
    }

    var type = typeProp.GetString();
    return type switch {
      "Rush" => JsonSerializer.Deserialize<Model.Events.Rush>(root.GetRawText(), options),
      "Spawn" => JsonSerializer.Deserialize<Model.Events.Spawn>(root.GetRawText(), options),
      "Trigger" => JsonSerializer.Deserialize<Model.Events.Trigger>(root.GetRawText(), options),
      "Preload" => JsonSerializer.Deserialize<Model.Events.Preload>(root.GetRawText(), options),
      _ => throw new JsonException($"Unknown Type: {type}")
    };
  }

  public override void Write(Utf8JsonWriter writer, Model.Event value, JsonSerializerOptions options) {
    JsonSerializer.Serialize(writer, value, value.GetType(), options);
  }
}
