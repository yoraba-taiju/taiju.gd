# CLAUDE.md — 巻き戻し機構の内部

`Util/Reversible/` 配下の実装そのものを触るときのための資料。**巻き戻し可能な敵やエフェクトを書くだけなら、ここを読む必要はない**（ルートの [CLAUDE.md](../../../CLAUDE.md) の契約だけで足りるように作ってある）。読むべきなのは `Clock` / `Dense` / `Sparse` / `SparseArray` / `ReversibleCompanion` の中身を変更するとき。

## 全体像

3 層に分かれている。

```
Clock                 … 時間の権威。tick と leap の代数だけを持ち、値のことは知らない
  ↑ 問い合わせ
IValue / IValueArray  … 履歴付きの値。Clock に「今どの tick の値を見るべきか」を尋ねる
  ↑ 埋め込み
ReversibleCompanion   … Godot ノードに寿命とコールバック振り分けを与える
```

Clock はバッファの存在を知らず、値は Godot を知らない。この分離のおかげで `Clock` と `Dense` だけを取り出して単体で動かせる（後述の検証はそれを利用している）。

## Clock — tick と leap の代数

`Clock.cs`。公開操作は `Tick` / `Back` / `Leap` の 3 つだけ。

- `CurrentTick`: 論理フレーム。`TickTime = 1/60`。
- `CurrentLeap`: 「巻き戻して別の未来へ分岐した回数」。
- `HistoryBegin`: `Tick()` が `max(HistoryBegin, CurrentTick - HistoryLength + 1)` で維持する。単調非減少。
- `HistoryLength = 256`。

### `Back()` が `>` である理由（`>=` にしてはいけない）

```csharp
public bool Back() {
  if (CurrentTick > HistoryBegin) { CurrentTick--; return true; }
  return false;
}
```

256 スロットのリングが保持できるのは **256 個の相異なる tick 値 `[T-255, T]`**。最新 `T` から戻れるのは `T-255` まで、つまり **255 歩**。256 歩目の `T-256` は `T` と 256 の差なので**同じスロット**に落ち、そこは既に最新の値で上書き済み。

実測（`Clock` と `Dense` をスクラッチに複製し、比較演算子だけ差し替えたもの）:

```
=== Back() が > の場合 ===
  戻れた回数 = 255 (tick 1000 -> 745)   Back() が false を返して停止

=== Back() が >= の場合 ===
  !! 256 歩目で例外: InvalidOperationException: Can't access before value born.
     そのときの CurrentTick = 744

tick 1000 のスロット = 232   ← 最新
tick  745 のスロット = 233   ← 255 歩目（到達可能な最古）
tick  744 のスロット = 232   ← 256 歩目。最新と同じスロット
```

つまり `-1` は恣意的な選択ではなく**リングのサイズから構造的に決まる**。`Dense.Ref` の `currentTick < HistoryBegin` チェックが無ければ、例外ではなく**最新 tick の値が黙って返る**。

`Clock` 自体はバッファを知らないので、**この 1 行が全 `Dense`/`Sparse`/`SparseArray` をまとめて範囲内に閉じ込めている唯一の砦**。

### `historyBranches_` — leap ごとの有効期限表

これが巻き戻し機構で一番非自明な部分。

**解いている問題**: `Dense` は `entries_[tick % 256]` なので、巻き戻して分岐すると**捨てたはずの未来のデータがスロットに残る**。leap 0 で tick 100 まで進め、tick 60 まで戻って分岐した場合、スロット 61〜100 には leap 0 の未来の値が残っている。この状態で leap 1 の tick 70 で「leap 1 では書かれていない値」を素朴に読むと、**もう起こらなかった未来の値**が返る。正しい答えは「分岐点 tick 60 の値」（分岐後に誰も書いていないなら凍っているはず）。

**意味**: `historyBranches_[L]` = **leap L に記録されたデータが有効な最後の tick**（＝ leap L の有効期限）。

- 現在の leap は `uint.MaxValue`（まだ分岐していない＝どこまでも有効）。コンストラクタの `historyBranches_[0] = uint.MaxValue` はこれ。
- `AdjustTick(lastTouchLeap, tick) = min(historyBranches_[lastTouchLeap], tick)` が「その値の最終書き込み leap の有効期限で、読みたい tick を頭打ちにする」変換。現在の leap なら `min(MAX, tick) = tick` で素通り。

各値は `lastTouchedLeap_` で「自分のデータはどの時間線のものか」だけを覚え、**読む瞬間に Clock へ「それ、どこまで有効？」と問い合わせる**という分担になっている。

### なぜ `Leap()` が過去の leap を全部 `Math.Min` で舐めるのか

```csharp
for (var i = (CurrentLeap >= HistoryLength) ? (CurrentLeap - HistoryLength) : 0; i <= CurrentLeap; ++i) {
  var idx = i % HistoryLength;
  historyBranches_[idx] = Math.Min(historyBranches_[idx], CurrentTick);
}
CurrentLeap++;
historyBranches_[CurrentLeap % HistoryLength] = uint.MaxValue;
```

**前回の分岐点より過去に巻き戻したとき**に効く。実測:

```
leap0 で 100 まで前進      leap=0 tick=100  branches[0]=MAX
leap() @ tick 60           leap=1 tick= 60  branches[0]=60   [1]=MAX
leap1 で 90 まで前進       leap=1 tick= 90  branches[0]=60   [1]=MAX
leap() @ tick 40           leap=2 tick= 40  branches[0]=40   [1]=40   [2]=MAX
                                       ↑ 60 だったものが 40 に引き下げられる
```

leap 0 の有効期限は一度 60 になったが、tick 40 で分岐し直したので **40 まで下がる**。tick 40 から先に進む新しい時間線では、leap 0 の tick 41〜60 のデータはもう通ってこないから。leap 1 も同様。

したがって `historyBranches_[L]` は**単調非増加**で、更新が `Math.Min` になる。性質としては「一度でも tick t で分岐したら、それ以前に離脱した全ての leap の有効期限も t 以下になる」。

効果の実測:

```
leap2 で 50 まで前進       leap=2 tick=50
  x.Ref at tick 50 = 40        ← 分岐点 40 の値で凍結。正しい
  (branches[0] が 60 のままなら 50 = 捨てた未来の値が返っていた)
```

### 本当の狙い — 遅延無効化

素朴にやるなら、leap した瞬間に**全 `Dense`/`Sparse` を走査して無効スロットを消す**必要がある。敵・弾・パーティクル全部が対象なので、巻き戻すたびに（全オブジェクト × 256 スロット）の処理が走る。

`historyBranches_` があると **leap のコストは `Clock` 内の O(HistoryLength) ループだけ**で済み、各値は次に読まれた/書かれた瞬間に初めて自分のデータの古さに気づく。**lazy invalidation** であり、読まれない値のコストはゼロ。巻き戻しを常用するゲームではここが効く。

### `HistoryLength` が兼任している役割

変更すると全体に波及する。少なくとも次の 4 つを兼ねている。

1. `Dense` / `Sparse` / `SparseArray` のリング長
2. `historyBranches_` の **leap 方向**のリング長
3. `Destroy` した死体を `QueueFree` せず保持する期間（`ClockNode.ProcessDestroy`）
4. 巻き戻し可能時間（`(HistoryLength - 1) / 60` ≒ 4.25 秒）

1 と 2 は**分離できない**。`Sparse`/`SparseArray` が `HistoryLength` 個で足りるのは「1 tick につきエントリは高々 1 個」だから。`historyBranches_` が leap 方向に `HistoryLength` 個で足りるのも同じ理屈（leap は最低 1 tick 時計を動かす、という前提）に依存している。よって `HistoryLength` を 10000 にするなら `historyBranches_` も 10000 以上必要。

**この「leap は最低 1 tick 時計を動かす」という前提は現在破れている**。→ [#8](https://code.ledyba.org/yoraba-taiju/taiju.gd/issues/8)

なお `HistoryLength` は 2 の冪である必要はない。履歴系は全て `%` 演算（`RingBuffer` だけはマスク方式だが別クラスで自前に `CalcMinimumPow2` しており無関係）。

## IValue — 履歴付きの値

`Value/`。`T : struct` 限定。`Ref`（読み） / `Mut`（書き）の 2 つの `ref` プロパティだけを公開する。

| 型 | 構造 | 用途 |
|---|---|---|
| `Dense<T>` | `T[256]`、tick ごとに 1 スロット | 毎フレーム変わる値（位置など） |
| `Sparse<T>` | `(tick, value)` のリング | `Mut` したときだけ記録が増える。低頻度更新 |
| `Meta<T>` | 履歴なし | 巻き戻し対象外であることを明示するため |
| `DenseArray<T>` / `SparseArray<T>` | 配列版。`Ref` は `ReadOnlySpan<T>`、`Mut` は `Span<T>` | 弾幕・パーティクル |
| `DenseClone<T>` | 不変スナップショット | `SoraClone` のリプレイ |

### `Mut` の埋め戻し

`Mut` は単なる書き込み口ではない。leap をまたいだ最初の書き込みで、**分岐点の値で間のスロットを埋める**。

```csharp
if (clock_.CurrentLeap != lastTouchedLeap_) {
  var branch = clock_.BranchTickOfLeap(lastTouchedLeap_);
  var v = entries_[branch % Clock.HistoryLength];
  for (var i = branch + 1; i <= currentTick; i++) entries_[i % Clock.HistoryLength] = v;
  ...
}
```

読みだけなら `Ref` のたびに clamp すれば済むが、書き込むと間のスロットに残骸が残ったままでは後で困るため。実測:

```
x.Mut = 999 (@tick 50)
x.Ref at tick 45 = 40    ← 41..50 が分岐点の値 40 で埋められている
                            (埋めていなければ leap0 の残骸 45 が見えた)
```

同一 leap 内でも `lastTouchedTick_` から `currentTick` までを同様に埋める（値が飛び飛びに書かれても間が埋まる）。`clonerFn_` が指定されていれば代入ではなくそれを使う（参照型メンバを持つ struct 用）。

**帰結**: 読むだけなら必ず `Ref` を使うこと。`Mut` は O(経過 tick) の副作用を持つ。

### `Sparse` / `SparseArray`

`(tick, value)` の昇順リング。`Ref` は `AdjustTick` した tick で `UpperBound` の二分探索をかけ、その tick 以前の最新エントリを引く。`Mut` は `LowerBound` で挿入位置を決め、必要なら最古を捨てて `entriesBeg_` を進める。

`SparseArray<T>` は `storage_ = new T[HistoryLength * size]` と `ticks_ = new uint[HistoryLength]` で、エントリ 1 個が「1 tick 分の配列全体」。弾を 1 発動かすたびに 64 発分がコピーされる構造なので、**`Mut` の呼び回数を最小化する**設計になっている（`BulletServer.ProcessBullets` が `Ref` で回して必要なときだけ `Mut` を取るのはこのため）。

**`Span` は `Mut` を呼ぶと無効化されうる**。`items_.Mut` した後は `items_.Ref` を取り直すこと（`ReversibleParticle3D._ProcessForward` に実例）。

## ReversibleCompanion — ノードへの埋め込み

`Godot/Companion/ReversibleCompanion.cs`。Godot のノードは多重継承できないので、共通ロジックを struct に置き、`ReversibleNode3D` / `ReversibleRigidBody3D` / `ReversibleAnimationPlayer` / `ReversibleAnimationTree` などの薄いラッパーが埋め込む。**新しい巻き戻し可能ノード型を足すときは既存ラッパーをコピーして Companion を埋める**のが定型。

- `Ready()` で `bornTick_` を記録し、`integrateTime_`（`Dense<double>`）を作る。
- `Process()` が `ClockNode.Direction` を見てコールバックを振り分ける（表はルート CLAUDE.md）。
- `Clock.CurrentTick < bornTick_` になったら `QueueFree()`。**生成前まで巻き戻ったノードは本当に消える**（`Destroy` の墓場とは別経路）。

### Destroy / Rescue の内部

`ClockNode` が `RingBuffer<Grave>`（容量 16384）を持つ。

1. `Destroy()` → `QueueDestroy` で `{ DestroyedAt = CurrentTick, Node }` を積み、`Visible = false` + `ProcessMode = Disabled`。**ノードは生かしておく**。`Rush` 以外には `_OnDestroy` を `PropagateCall` で伝播。
2. `ProcessRescue()`（`Back()` 成功時に呼ばれる）が末尾から見て `DestroyedAt >= CurrentTick` なら `Rescue()`。
3. `ProcessDestroy()`（毎フレーム）が先頭から見て `DestroyedAt + HistoryLength < CurrentTick` になったものだけ本当に `QueueFree()`。

墓場が LIFO/FIFO の両端から操作されるのは、**破壊は tick 順に積まれる**ので末尾＝最新、先頭＝最古という順序が保証されるため。`RingBuffer.AddLast` は満杯で例外を投げるが、実コストは 16384 エントリ（1 個 16 バイト程度）ではなく**保持され続けるノード本体**の方で、それは `HistoryLength` で決まる。

## 検証環境

上記の実測は `Clock.cs` と `Value/Dense.cs` をスクラッチのコンソールプロジェクトに複製して再現したもの。`Dense.cs` は Godot 非依存、`Clock.cs` も `using Godot;` を落とすだけ（`GD.PrintRich` は未使用の `DebugBranch` からしか呼ばれない）でビルドが通る。

**この 3 つは TODO のテストハーネスができたら真っ先にテストケースにすべき性質**:

1. `Back()` の到達回数がちょうど `HistoryLength - 1` であること
2. 分岐点より過去への再分岐で、古い leap の有効期限が引き下げられること
3. leap をまたいだ `Mut` が間のスロットを分岐点の値で埋めること
