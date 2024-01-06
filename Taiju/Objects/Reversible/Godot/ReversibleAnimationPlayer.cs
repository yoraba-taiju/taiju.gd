using Godot;
using Taiju.Objects.Reversible.Godot.Companion;

namespace Taiju.Objects.Reversible.Godot;

public abstract partial class ReversibleAnimationPlayer : AnimationPlayer, IReversibleNode {
  private ReversibleCompanion<ReversibleAnimationPlayer> comp_;

  /*
   * Members
   */
  protected Clock Clock => comp_.Clock;
  protected ClockNode ClockNode => comp_.ClockNode;
  public bool IsAlive {
    get => comp_.IsAlive;
    set => comp_.IsAlive = value;
  }

  public override void _Ready() {
    comp_.Ready(this);
    PlaybackActive = true;
  }

  /*
   * Impls
   */
  public override void _Process(double delta) {
    comp_.Process(this, delta);
    PlaybackActive = true;
  }
  
  /*
   * Helpers
   */

  public virtual void Destroy() {
    comp_.Destroy(this);
  }

  public void Rescue() {
    comp_.Rescue(this);
  }

  /*
   * Default overrides
   */

  public abstract bool _ProcessForward(double integrateTime, double dt);

  public abstract bool _ProcessBack(double integrateTime);

  public virtual bool _ProcessLeap(double integrateTime) {
    Seek(integrateTime, true, true);
    return true;
  }

  public virtual void _ProcessRaw(double integrateTime) {
  }
}
