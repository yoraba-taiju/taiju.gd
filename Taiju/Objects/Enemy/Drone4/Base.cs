using Godot;
using Taiju.Objects.Effect;
using Taiju.Util.Reversible.Value;

namespace Taiju.Objects.Enemy.Drone4;

public abstract partial class Base<TParam> : EnemyBase
  where
TParam: struct
{
  [Export] private int initialShield_ = 6;
  
  // Nodes
  protected AnimationPlayer AnimPlayer;
  protected StarDust StarDust;

  public struct RecordType {
    public int Shield;
    public TParam Param;
  }

  public Dense<RecordType> Record;

  public override void _Ready() {
    base._Ready();
    Record = new Dense<RecordType>(Clock, new RecordType {
      Shield = initialShield_,
      Param = new TParam(),
    });
    AnimPlayer = GetNode<AnimationPlayer>("Body/Drone4/AnimationPlayer")!;
    AnimPlayer.CurrentAnimation = "Shaft_Rotate";
    StarDust = GetNode<StarDust>("Body/StarDust")!;
  }
  
  protected override ref int ShieldMut => ref Record.Mut.Shield;
  
}
