using Godot;

namespace Taiju.Scenes.Model.Events;

public class Preload : Event {
  [Export(PropertyHint.File, "*.tscn")] public string Path { get; set; }

  public PackedScene Scene;
}
