using System.Linq;

namespace Taiju.Scenes.Loader.JsonConverters;

using System;
using System.Text.Json;
using System.Text.Json.Serialization;

public class StageConverter : JsonConverter<Model.Stage> {
  public override Model.Stage Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
    using var jsonDoc = JsonDocument.ParseValue(ref reader);
    var root = jsonDoc.RootElement;

    if (!root.TryGetProperty("Events", out var eventsProp) || eventsProp.ValueKind != JsonValueKind.Array) {
      throw new JsonException("Events property is missing or invalid.");
    }

    var stage = new Model.Stage {
      Events = eventsProp
        .EnumerateArray()
        .Select(obj => obj.Deserialize<Model.Event>(options))
        .ToArray(),
    };

    return stage;
  }

  public override void Write(Utf8JsonWriter writer, Model.Stage value, JsonSerializerOptions options) {
    JsonSerializer.Serialize(writer, value, value.GetType(), options);
  }
}
