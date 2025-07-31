#nullable enable
using Godot;

namespace Taiju.Scenes.Model.Events;

public class Preload : Event {
  public required string Path { get; init; }

  public PackedScene? Scene { get; set; }
}
