namespace Taiju.Reversible.Gd; 

public interface IReversibleNode {
  public bool _ProcessForward(double integrateTime, double dt);

  public bool _ProcessBack(double integrateTime);

  public bool _ProcessLeap(double integrateTime);

  public void _ProcessRaw(double integrateTime);
}
