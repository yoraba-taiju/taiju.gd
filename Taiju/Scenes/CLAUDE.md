# CLAUDE.md — ステージパイプラインの内部

`Scenes/` 配下（と `addons/editor/`）の実装を触るときのための資料。敵の配置を変えるだけなら、ルートの [CLAUDE.md](../../CLAUDE.md) の「⛏️ Compile Stages を押す」だけ知っていれば足りる。

## 流れ

```
Stage.tscn（Godot 2D エディタで配置）
  └ Scenes/Base/*.gd の Event / Spawn / Rush / Trigger / Preload ノードを置く
     Spawn の子に Path2D を置くと軌道になる
  ↓ 「⛏️ Compile Stages」= Scenes/Compiler/stage_compiler.gd
Stage.tscn.json
  ↓ StageLoader（Loading.tscn）: JSON を読み、出現する .tscn を全部プリロード
  ↓ LoadingScreen: ロード完了後に Stage シーンを差し替え、Stager に Stage と ResourceManager を注入
Stager._ProcessForward: stagePosition が進み、X < stagePosition + 1280 のイベントを発火
```

## エディタ側のノード定義

`Scenes/Base/*.gd`。どれも `Node2D` を継承した `Event` の派生。

| class | export | 意味 |
|---|---|---|
| `Event` | — | 基底。位置しか持たない |
| `Spawn` | `path: String` (*.tscn) | 敵 1 体。子に `Path2D` を置くと軌道になる |
| `Rush` | — | `Spawn` をまとめる。子の `Spawn` 座標はこのノードからの相対 |
| `Trigger` | `id: String` | 演出フック（未使用）。`id` で演出を識別する |
| `Preload` | `path: String` (*.tscn) | 出現はしないがロードだけしておきたいもの |

## コンパイラ（`Scenes/Compiler/stage_compiler.gd`）

対象シーンは `ScenePaths` にハードコード。**ステージを増やしたらここに足す**。

- ルート直下の `Event` 派生ノードだけを拾い、**X 座標昇順にソート**して `Events` 配列にする。
- `Rush` は子の `Spawn` を `Spawns` にネストする。孫は見ない。
- `Spawn` の子 `Path2D` は `Curve` に変換されるが、**逆順に展開され `in` と `out` が入れ替わる**:
  ```gdscript
  for idx in range(curve.point_count - 1, -1, -1):
      curves.append({
          "Position": ...,
          "In":  compile_point(curve.get_point_out(idx)),   # 入れ替え
          "Out": compile_point(curve.get_point_in(idx)),
      })
  ```
  エディタ上では左から右へ描くが、ゲーム中は右から左へ進むため。**軌道を編集したら向きに注意**。
- 出力先は `<scene_path>.json`。

起動経路は 2 つ（`addons/editor/plugin.gd` が両方登録）:

- `compile_button.gd` — 2D エディタ上部のボタン。`EditorInterface.save_scene()` してからコンパイル。
- `exporter.gd` — `EditorExportPlugin._export_begin`。エクスポート時に自動。

## JSON のデシリアライズ

`Scenes/Model/`。`EventConverter` が `"EventType"` フィールドを見て `Rush`/`Spawn`/`Trigger`/`Preload` に振り分ける多態デシリアライザ。`StageDeserializer.Load` は Godot の `FileAccess` で読む（`res://` を解決するため）。

`Stage.ForStager()` が `Preload` を落とした配列を作る。**`StageLoader` は全イベント（`Preload` 含む）を見てプリロード対象を集め、`Stager` には `Preload` を除いたものを渡す**、という役割分担。

## StageLoader / ResourceManager

- `StageLoader._Process` は **`#if DEBUG` で同期ロード、リリースでは `LoadThreadedRequest` の非同期ロードと実装が丸ごと分かれている**。片方だけ直すと本番だけ壊れるので注意。
- `CommonPreloadScenes` にソラ・弾・エフェクトなど常駐物が列挙されている。ステージ JSON に出てこないが必要なものはここ。
- `ResourceManager` はロード済み `PackedScene` に加えて**インスタンス済みノードを 1 個先読みキャッシュ**する。`Instantiate` はキャッシュを返してから次の 1 個を即座に作り置きする:
  ```csharp
  entry.NodeCache ??= resource.Instantiate<T>();
  var node = (T)entry.NodeCache;
  entry.NodeCache = resource.Instantiate<T>();   // 次回ぶんを先に作っておく
  return node;
  ```
  出現時のスパイクを避けるため。**`Arrow.tscn` だけは明示的に除外されている**（`Add` の中でパス直書きの比較）。キャッシュを作ると終了時に `WARNING: 1 ObjectDB instance was leaked at exit` が必ず出るため。**原因は未特定**だが、`ReversibleTubeTrail` の `private MeshInstance3D meshInstance_ = new();` が `Instantiate()` 時点で親なし Node を作り `AddChild` は `_Ready()` の中、という構造が候補（ツリーに入らないキャッシュでは孤児のまま残る）。詳細と直し方の候補は [#25](https://code.ledyba.org/yoraba-taiju/taiju.gd/issues/25)。
- `path` は `uid://` でも来るので `NormalizePath` が `ResourceUid.UidToPath` で解決する。

## Stager

`Scenes/Stages/Stager.cs`（抽象）→ `Scenes/Stages/Stage01/Stager.cs`。`OnTrigger` の実装が各ステージの責務。

**`Trigger` はボス出現・BGM 切り替え・カメラ演出などアドホックな演出を C#/GDScript 側のスクリプトに丸投げするためのフック**という設計意図（現状 `NotImplementedException`。演出をどう巻き戻すかも未解決）。

### X 座標は「いつ」だけを決める

```csharp
stagePosition += StageSpeed * dt;            // StageSpeed = 120.0
var invokePosition = stagePosition + StageWidth;   // StageWidth = 1280
var basePos = new Vector2((float)-stagePosition, 0.0f);
while (stageIndex < Stage.Events.Length && Stage.Events[stageIndex].X < invokePosition) { ... }
```

イベント X で発火し、そのとき `basePos = -stagePosition` を足すので、**湧く場所は常に画面右端**。どこに出るかは `Rush` からの相対座標（と `Spawn` の相対座標）で決まる。つまり **X 座標と時間は単位が違うだけで等価**。

### 座標変換

ステージ座標系は 1280x720 の 2D。`ScreenToWorld()` がカメラのレイキャストで Z=0 平面上の 3D 座標に変換する。`Curve3D` の制御点も同じ変換を通すが、`In`/`Out` は**方向ベクトルなので画面中心 `halfStageSize` を足してから変換**している（原点のずれを打ち消すため）。

`LoadSpawn` は **`AddChild` の前に `Position` を設定する**。`ReversibleCompanion.Ready` が `_Ready` で初期位置を記録するため、順序を変えると初期値がずれる。

### 巻き戻しとの関係

`Stager` 自体も `ReversibleNode3D` で、`stagePosition` と `stageIndex` を `Dense<Record>` に持つ。**巻き戻すと出現位置も巻き戻る**。

**背景のスクロールはステージ進行と独立**。`Stage01/Forest.cs` は自分の `integrateTime` から uv オフセットを、`Common/Cloud.cs` は `initialX_ - t * speed` を計算する（どちらも `f(t)` 方式）。`Stager.StageSpeed` は背景速度と無関係なので、**背景速度の演出変更は敵の湧きタイミングに影響しない**。速度を時間で変えたい場合も、区分定数を解析的に積分すれば `f(t)` のまま書ける。
