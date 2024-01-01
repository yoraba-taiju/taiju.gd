namespace Taiju.Objects.Reversible.Godot;

public interface IReversibleNode {
  public bool IsAlive { get; set; }
  public void Destroy();
  public void Rescue();

  public bool _ProcessForward(double integrateTime, double dt);

  public bool _ProcessBack(double integrateTime);

  public bool _ProcessLeap(double integrateTime);

  public void _ProcessRaw(double integrateTime);
}
