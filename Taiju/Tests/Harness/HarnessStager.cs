using Godot;
using Taiju.Scenes.Model.Events;

namespace Taiju.Tests.Harness;

/**
 * ハーネス用の Stager。
 *
 * Stages.Stager は abstract なので骨格シーンにはスクリプトが付いていない (StageRoot が検査している)。
 * ハーネスはステージ進行を使わない ── 検体はテスト側が直接ツリーに置く ── ので、
 * ここは「Stager が要求する契約を最小限満たすだけ」の実装になっている:
 *
 *   - Stage        … Stager._ProcessForward が Stage.Events を無条件に読む
 *   - ResourceManager … Stager._ExitTree が無条件に Free() する。
 *                       EnemyBase._Ready も stager_.ResourceManager を読む
 *
 * どちらも _Ready より前に埋める必要があるが、Godot はノード生成時に引数を渡せない。
 * PackedScene.Instantiate() の時点では _Ready がまだ走らないので、
 * テスト側が Instantiate → ここのプロパティを設定 → ツリーへ追加、の順で組み立てる
 * (ReversibilityHarness.BootAsync を参照)。
 */
public partial class HarnessStager : Scenes.Stages.Stager {
  protected override void OnTrigger(Vector2 basePosition, Trigger trigger) {
    // ハーネスはイベントを流さないので呼ばれない。
  }
}
