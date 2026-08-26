using Godot;

namespace Taiju.Scenes.Stages;

/**
 * ステージの骨格シーン (Common/StageSkeleton.tscn) のルートに付くスクリプト。
 *
 * ステージのノードはほぼ全部が GetNode<T>("/root/Root/...") の絶対パス直書きで他ノードを掴むので、
 * 「ルート名が Root であること」と「Stager にスクリプトが付いていること」が暗黙の契約になっている。
 * 継承シーンは骨格から受け継いだ子ノードの改名・削除を禁じてくれるが、この 2 つは防げない:
 *   - 継承シーンのルートノードは派生側で自由に改名できる
 *   - 骨格の Stager はスクリプト無しの Node3D で、スクリプトを付けるのは派生シーンの仕事
 * 落ちるのは EnemyBase._Ready() などの遠い場所なので、ここで先に検査して原因を名指しする。
 *
 * _Ready ではなく _EnterTree で検査するのは、_Ready が子から先に (bottom-up) 呼ばれるため。
 * 子の GetNode が失敗し切った後では手遅れで、エラーの山の末尾にこのメッセージが出ることになる。
 * _EnterTree は親から先 (top-down) なので、どの子より早く出せる。
 */
public partial class StageRoot : Node3D {
  public override void _EnterTree() {
    base._EnterTree();
    if (Name != "Root") {
      GD.PushError(
        $"ステージのルートノードの名前は \"Root\" でなければならない (実際は \"{Name}\")。" +
        "全ノードが \"/root/Root/...\" の絶対パスで互いを掴んでいる。");
    }
    if (GetNodeOrNull<Stager>("Stager") == null) {
      GD.PushError(
        "Stager ノードに Stager 派生のスクリプトが付いていない。" +
        "骨格シーンはスクリプト無しの Node3D を置くだけなので、派生シーン側で付けること。");
    }
  }
}
