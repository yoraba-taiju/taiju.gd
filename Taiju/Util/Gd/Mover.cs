using Godot;

namespace Taiju.Util.Gd;

public static class Mover {
  public static Vector2 Follow(in Vector2 delta, in Vector2 velocity) {
    return delta.Normalized() * velocity.Length();
  }
  
  
}