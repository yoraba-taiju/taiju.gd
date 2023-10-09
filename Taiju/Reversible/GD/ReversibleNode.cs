namespace Taiju.Reversible.GD; 

public interface IReversibleNode {
  protected void _ProcessForward(double integrateTime, double dt) {
  }

  protected void _ProcessBack() {
  }

  protected void _ProcessLeap() {
  }
}
