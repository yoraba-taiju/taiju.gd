using Godot;
using Taiju.Util.Reversible.Godot;

namespace Taiju.Objects.Effect;

public partial class MagicCircle : ReversibleNode3D {
  [Export] private Color modulateColor_ = Colors.White;
  private Sprite3D circleSprite_;
  private Sprite3D elementSprite1_;
  private Sprite3D elementSprite2_;
  private Sprite3D elementSprite3_;
  private Sprite3D elementSprite4_;
  
  public override void _Ready() {
    base._Ready();
    circleSprite_ = GetNode<Sprite3D>("Circle")!;
    elementSprite1_ = GetNode<Sprite3D>("Element1")!;
    elementSprite2_ = GetNode<Sprite3D>("Element2")!;
    elementSprite3_ = GetNode<Sprite3D>("Element3")!;
    elementSprite4_ = GetNode<Sprite3D>("Element4")!;
    {
      circleSprite_.Modulate = modulateColor_;
      elementSprite1_.Modulate = modulateColor_;
      elementSprite2_.Modulate = modulateColor_;
      elementSprite3_.Modulate = modulateColor_;
      elementSprite4_.Modulate = modulateColor_;
    }
  }

  private void SetRotate(double integrateTime) {
    {
      var rot = Mathf.DegToRad((360.0/3) * integrateTime);
      elementSprite1_.Rotation = new Vector3(0, 0, (float)rot);
      elementSprite2_.Rotation = new Vector3(0, 0, -(float)rot);
    }
    {
      var rot = Mathf.DegToRad((360.0/5) * integrateTime);
      elementSprite3_.Rotation = new Vector3(0, 0, (float)rot);
      elementSprite4_.Rotation = new Vector3(0, 0, -(float)rot);
    }
  }

  public override bool _ProcessForward(double integrateTime, double dt) {
    SetRotate(integrateTime);
    return true;
  }

  public override bool _ProcessBack(double integrateTime) {
    SetRotate(integrateTime);
    return true;
  }
}
