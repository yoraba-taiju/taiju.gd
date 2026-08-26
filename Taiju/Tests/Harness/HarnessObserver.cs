using System;
using Godot;

namespace Taiju.Tests.Harness;

/**
 * ハーネスの観測点。ステージのルートの**最後の子**として挿し込む。
 *
 * テスト側から await で 1 フレーム進めて外から状態を読む、という素直なやり方は使えない。
 * gdUnit4 の await が返ってくるのはフレーム N の _Process が走る**前**で、
 * そこは「フレーム N の物理ステップは済んでいるが、ノードの _Process はまだ」という位置にある。
 * 物理で動くノード (ReversibleRigidBody3D 派生) はそこで 1 ステップ先の位置に居るので、
 * 記録した値と突き合わせると全 tick が丸ごと 1 tick ぶんずれる。
 *
 * Godot は _Process をツリー順に配るので、ルートの最後の子はどのノードよりも後に処理される。
 * ここから観測すれば、その tick の描画に出るのと同じ状態が見える。
 */
internal partial class HarnessObserver : Node {
  public Action OnProcessed { get; set; }

  public override void _Process(double delta) {
    OnProcessed?.Invoke();
  }
}
