# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

2D STG「ヨラバ・タイジュ」の Godot 実装。Godot 4.7 (Forward+) + C# (.NET 8, LangVersion 12)。
主人公の魔女「ソラ」が**時間を巻き戻す**能力を持つのがゲームの中核であり、**画面上のほぼ全オブジェクトが巻き戻し可能**であることがこのコードベースの最大の制約。

このファイルは「守らないと静かに壊れる契約」だけを置いている。内部の仕組みは必要になったときに読む:

- 巻き戻し機構の内部 → [Taiju/Util/Reversible/CLAUDE.md](Taiju/Util/Reversible/CLAUDE.md)
- ステージパイプラインの内部 → [Taiju/Scenes/CLAUDE.md](Taiju/Scenes/CLAUDE.md)
- 未実装・未解決の課題 → [TODO.md](TODO.md)

## コマンド

```bash
cd Taiju && dotnet build
```

```bash
cd Taiju && dotnet test
```

- ゲーム本体の実行・シーン編集は Godot エディタから（`Taiju/project.godot` を開く）。メインシーンは `Scenes/Stages/Stage01/Loading.tscn`。
- **ステージ JSON の再生成**: Godot エディタの 2D 画面上部にある「⛏️ Compile Stages」ボタン。`Scenes/Stages/Stage01/Stage.tscn` を編集したら押す。**押さないと `Stage.tscn.json` が古いままでゲームに反映されない**。エクスポート時は自動で走る。
- 単一テスト実行: `dotnet test --filter FullyQualifiedName~DenseValueTest`

## テストの契約

テストは gdUnit4Net で、**`Taiju.csproj` に同居している**（`Taiju/Tests/`）。gdUnit4 のテストアダプタはテスト csproj のあるディレクトリを Godot プロジェクトルートとみなすので、別プロジェクトには分離できない。テスト関連の参照は `Debug` 構成にだけ効き、`ExportDebug`/`ExportRelease` では `Tests/` ごと除外される。

- **`GODOT_BIN` 環境変数が必須**。未設定だと純ロジックのテストも含めて 1 件も走らず exit 1 になる（`.runsettings` は環境依存のパスを持たないので、各自で設定する）。

  ```bash
  export GODOT_BIN=/path/to/Godot_v4.7.1-stable_mono_win64_console.exe
  ```

- テストクラスに `[TestSuite]`、テストメソッドに `[TestCase]`、アサーションは `AssertThat(x).IsEqual(y)` / `AssertThrown(() => ...).IsInstanceOf<T>()`。
- **Godot に触るテストだけ `[RequireGodotRuntime]` を付ける**。付けたものはヘッドレスの Godot を起動して実行され、付けないものはエンジン抜きで走る（速い）。ノード生成・`res://` の読み込み・`RandomNumberGenerator` のようなネイティブクラスは付ける側。
- **基底クラスから継承した `[TestCase]` は discover されない**。型引数違いで同じ検査を回したいときは、共有ロジックを基底の普通のメソッドに置き、各 `[TestSuite]` で `[TestCase] public void Foo() => FooImpl();` と並べる（`Tests/Util/Reversible/Value/` が実例）。
- `gdunit4_testadapter_v5/` はアダプタが実行のたびに自動生成する。`.gitignore` 済み。
- **`dotnet test` は Godot エディタを一度ヘッドレス起動する**副作用があり、そのとき一部の `.cs` が Godot に書き換えられる（→ [TODO.md](TODO.md)）。

## 巻き戻しの契約

これを守れば内部を知らなくても巻き戻し可能なオブジェクトが書ける。

### `Ref` / `Mut`

履歴付きの値は `Util/Reversible/Value/`（`Dense<T>` / `Sparse<T>` / `Meta<T>`）と `ValueArray/`（`DenseArray<T>` / `SparseArray<T>`）にある。`T : struct` 限定で、`Ref`（読み）と `Mut`（書き）の 2 つの `ref` プロパティだけを公開する。

| 型 | 使うとき |
|---|---|
| `Dense<T>` | 毎フレーム変わる値（位置など） |
| `Sparse<T>` | たまにしか変わらない値（フラグ、状態機械） |
| `Meta<T>` | 巻き戻し**しない**ことを明示したい値 |
| `DenseArray<T>` / `SparseArray<T>` | 配列版。`Ref` が `ReadOnlySpan<T>`、`Mut` が `Span<T>` |

- **読むだけなら必ず `Ref`**。`Mut` は書き込みに加えて埋め戻しや leap 分岐処理の副作用があり、経過 tick 数に比例したコストを持つ。
- **`Span` / `ref` は `Mut` を呼ぶと無効化されうる**。`items_.Mut` した後は `items_.Ref` を取り直すこと（`ReversibleParticle3D._ProcessForward` に実例）。

### ノードの書き方

`ReversibleNode3D` / `ReversibleRigidBody3D` / `ReversibleAnimationPlayer` / `ReversibleAnimationTree` のいずれかを継承する。`_Process` は乗っ取られていて、代わりに以下が呼ばれる:

| `ClockNode.Direction` | 呼ばれるコールバック |
|---|---|
| Forward | `_ProcessForward(integrateTime, dt)` |
| Back | `_ProcessBack(integrateTime)` |
| Stop かつ Leaped | `_ProcessLeap(integrateTime)` |
| Stop（通常） | `_ProcessBack(integrateTime)` |

- `integrateTime` は**そのノード固有の累積時間**。巻き戻すと巻き戻る。`f(t)` 方式の `t` はこれを使う。
- 戻り値 `bool` は「処理済みか」。**`false` を返すと続けて `_ProcessRaw(integrateTime, dt)` が呼ばれる**（巻き戻しの枠外で毎フレーム動きたいもの用。`BulletServer` が全部 `false` を返して `_ProcessRaw` に集約しているのが典型）。
- `_ProcessBack` / `_ProcessLeap` では記録から Godot ノードへ書き戻す。既存実装は `LoadCurrentStatus()` という名前で統一されている。
- `_Ready()` では必ず `base._Ready()` を先に呼ぶ（Companion の初期化がそこで走る）。
- **新しい巻き戻し可能ノード型が要るときは、既存ラッパーをコピーして `ReversibleCompanion<T>` を埋める**のが定型。

### `QueueFree()` を直接呼んではいけない

`Destroy()` を使う。ノードはすぐには消えず、巻き戻したら `Rescue()` で復活する。子孫には `_OnDestroy` / `_OnRescue` が伝播する。約 4 秒（`HistoryLength` 分）経ってから本当に解放される。

### 巻き戻せる長さ

約 **4.25 秒**（`HistoryLength = 256` のうち戻れるのは 255 tick）。これは仕様。魔素の量でも戻せる量は決まるが、4 秒は超えない。

## 状態表現の 2 方式

コード内に 2 つのアプローチが共存している。新しい敵を書くときはどちらかを選ぶ。

1. **履歴記録方式**: `Dense`/`Sparse` に位置や状態機械を毎フレーム記録する。例: `Objects/Enemy/Drone4/`（Kamikaze の状態機械）、`Objects/Effect/MagicElementItem.cs`。
2. **`f(t)` 方式**: 位置を `integrateTime` の関数として書く。状態を持たないので `_ProcessBack` すら不要なことが多い。例: `Objects/Enemy/Drone1/FollowCurve.cs`（`Curve.SampleBaked(t * speed)`）、`Objects/BulletServer/`（`IBullet.AttitudeAt(t)` — `[Pure]` が付いているのはこの契約のため）、`Stage01/Forest.cs`、`Common/Cloud.cs`。

**選択基準はメモリではなくゲーム性**。今は自機の位置を見て動く敵（`Drone4/Kamikaze`、`Drone2/Chase` など）が多く、これだと「パターンを覚える」より「敵をよく見て駆け引きする」ゲームになってパターンが組みにくい。パターンシューティングにしたいので結果的に `f(t)` 寄り・ハイブリッド寄りになる見込みだが、**メモリ削減のために動きを簡素にする必要はない**。分岐が必要なら「t の関数 + 分岐した tick だけ記録」のハイブリッドでよい。

## 物理

`ReversibleRigidBody3D` は Back/Stop 中に `Freeze = true` にして Godot 物理を止める。速度は `_IntegrateForces` で `Record.Ref` から与える（`Drone0/Straight.cs`、`Drone4/Base.cs` 参照）。衝突判定は `BodyEntered` シグナル。物理レイヤは `project.godot` の `[layer_names]` に定義（Terrain / Witch / WitchBullet / Enemy / EnemyBullet）。

**ただし Godot 物理は実質シミュレーションとしては使っていない**（速度は毎フレーム記録から上書きしているだけ）。当たり判定だけのために RigidBody3D を使っている状態で、巻き戻しを多用すると判定が壊れることがある。自前の当たり判定への置き換えは許容されている。**ノード形状の制約に引きずられて設計を歪めないこと**。

## ノードツリーの規約

ほぼ全ノードが `_Ready()` で `GetNode<T>("/root/Root/...")` の**絶対パス直書き**で他ノードを掴む。`Scenes/Stages/Stage01/Main.tscn` のツリー構造がそのまま暗黙の契約になっている：

```
/root/Root
  ├ Player           … 入力を読み state を持つだけ（Node3D だが描画しない）
  ├ Clock            … ClockNode
  ├ MainCamera       … Util/Godot/Camera（HalfScreenSize を公開）
  ├ Field
  │  ├ Witch/Sora, WitchEffect, WitchBullet/SoraBulletServer
  │  ├ Enemy/DefaultRush, EnemyEffect
  │  ├ EnemyBullet/{Red,Blue}CircleBulletServer
  │  └ HUD/{Score, SpellGauge}
  └ Stager           … ステージ進行 + Forest（背景）
```

**このパスを変えると広範囲が壊れる**。ノードを移動する場合は `grep -r '/root/Root'` で全参照を洗うこと。

時間を進めるのは `Objects/ClockNode.cs` ただ一つ。`Player` の `CurrentClockOperation` を毎フレーム見て `Tick`/`Back`/`Leap` を呼ぶ。**入力 → `Player` → `ClockNode` → 各ノード、という一方向の流れ**を崩さないこと。

## ゲームデザインの不変条件

調整や最適化で壊しやすいもの。

- **巻き戻しは回避手段であり攻撃手段でもある**。習熟度で使い分けが変わるのが設計の狙い。初心者は主に被弾の回避として使い、上級者は `SoraClone` を増やして敵を多く倒しスコアを稼ぐためにも使う。**どちらか一方に寄せた最適化・調整はしないこと**。
- **分身の寿命 = 巻き戻した深さ**。`SoraClone` は `DenseClone<Record>` を再生するので、深く戻すほど長く残り、浅く戻すとすぐ消える。**タダで長い分身を得られてはいけない**という自己抑制がここに掛かっている。
- **魔素は巻き戻しても戻らない**。`Player.state_` は `Dense` ではない素の struct。巻き戻しの通貨なので意図的。

### 魔素（Magic Element）

ゲーム中の呼称は「**魔素**」。コード上の識別子も `MagicElement` 系で揃っている（`Player.NumMagicElements` / `MagicElementItem` / `numMagicElementsPerTick_`）。**MP とは呼ばない**。

- 敵を倒すと `MagicElementItem` が落ち、ソラに吸われて増える。
- 巻き戻し 1 tick ごとに `numMagicElementsPerTick_` 消費、尽きると `ClockOperation.Stop`。
- **巻き戻しの発動自体にコストは無い**。減算は `Player.OnActionByClock(BackTick)` の 1 箇所のみで、`Clock.Back()` に成功したときしか呼ばれない。`numMinimumMagicElementsToBack_` は減算されない敷居値。
- **守るべき不変条件**: 「魔素はあるのに戻せない」が起きないよう `SpellGauge.MaxItems <= (Clock.HistoryLength - 1) * numMagicElementsPerTick_` であること。**現状これは 1 だけ破れている**（[TODO.md](TODO.md)）。

## その他

- `Taiju/assets/` は**別リポジトリ**（`yoraba-taiju/assets.git`）を中に置いているだけで submodule ではない。親の `.gitignore` で除外されている。
- `.godot/` は Git 管理外。
- インデントは 2 スペース。private フィールドは `camelCase_`（末尾アンダースコア）。
