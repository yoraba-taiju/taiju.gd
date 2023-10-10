namespace Taiju.Reversible.GD; 

public interface IReversibleNode {
  public bool _ProcessForward(double integrateTime, double dt);

  public bool _ProcessBack();

  public bool _ProcessLeap();

  public void _ProcessRaw(double integrateTime);
}
