using System;
using Godot;

namespace Taiju.Tests.Harness;

/**
 * ハーネスの観測点。
 *
 * テスト側から await で 1 フレーム進めて外から状態を読む、という素直なやり方は使えない。
 * gdUnit4 の await が返ってくるのはフレーム N の _Process が走る**前**で、
 * そこは「フレーム N の物理ステップは済んでいるが、ノードの _Process はまだ」という位置にある
 * (Engine.GetProcessFrames() が観測時 N・ノード内 N-1 になることで確認した)。
 * 物理で動くノード (ReversibleRigidBody3D 派生) はそこで 1 ステップ先の位置に居るので、
 * 記録した値と突き合わせると全 tick が丸ごと 1 tick ぶんずれる。
 *
 * そこでスナップショットはこのノードの _Process から取る。**どのノードよりも後に回る**ことが
 * 必要条件で、それを 2 つで担保している:
 *
 *   - ProcessPriority を int.MaxValue にする (Godot は _Process を優先度順に配り、
 *     同じ優先度の中ではツリー順になる)
 *   - ステージのルートの最後の子として挿す
 *
 * 前者だけで足りるが、後者も満たしておけば優先度の扱いが変わっても壊れない。
 * ここが崩れると症状は「全 tick が 1 tick ずれる」で、原因がいちばん読みにくい赤になる。
 */
internal partial class HarnessObserver : Node {
  public HarnessObserver() {
    ProcessPriority = int.MaxValue;
  }

  public Action OnProcessed { get; set; }

  public override void _Process(double delta) {
    OnProcessed?.Invoke();
  }
}
