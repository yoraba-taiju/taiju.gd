namespace Taiju.Reversible.GD; 

public interface IReversibleNode {
  public void _ProcessForward(double delta) {
  }

  public void _ProcessBack() {
  }

  public void _ProcessLeap() {
  }
}
