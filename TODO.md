# TODO

「ヨラバ・タイジュ」の未着手・未解決事項。

- 契約・規約 → [CLAUDE.md](CLAUDE.md)
- 巻き戻し機構の内部 → [Taiju/Util/Reversible/CLAUDE.md](Taiju/Util/Reversible/CLAUDE.md)
- ステージパイプラインの内部 → [Taiju/Scenes/CLAUDE.md](Taiju/Scenes/CLAUDE.md)

## テスト

- [ ] **`TaijuTest` を復活させる**（今はビルドすら通らない）
  - `TaijuTest.csproj` の TFM が `net6.0`、`Taiju.csproj` が `net8.0` で NU1201。`net8.0` に上げる。
  - テストの `using` が旧名前空間 `Taiju.Objects.Reversible.*` のまま。`Taiju.Util.Reversible.*` に置換する（`TaijuTest/Util/*` だけは既に新しい）。
  - ただし直す前に下の「Godot 込みのテスト方法」を先に決めたい。純粋ロジック用の枠組みと二重に用意したくない。
- [ ] **Godot 込みのテスト方法を導入する**
  - 現状の NUnit + `dotnet test` は `Clock` / `Dense` / `Sparse` / `RingBuffer` のような **Godot に依存しない純粋ロジックしかテストできない**。
  - 本当にテストしたいのは「ノードを 100 tick 進めて 50 tick 巻き戻したら状態が一致するか」という**巻き戻しの往復性**で、これには `ReversibleCompanion` / `ClockNode` / シーンツリーが要る。
  - 候補: GUT / GdUnit4（C# 対応、エディタ内 & CLI 実行可）などのアドオン、あるいは Godot をヘッドレス起動して自作のテストシーンを走らせる方式。要調査。
  - 欲しい性質: **同じ入力列を Forward → Back → Leap で流して、記録した状態と復元した状態が一致することを機械的に検査する**汎用ハーネス。敵を 1 体足すたびに手で確認するのは無理がある。
  - 最初に入れるべきケース 3 つは [Taiju/Util/Reversible/CLAUDE.md](Taiju/Util/Reversible/CLAUDE.md) の末尾に記載（検証済みの性質なので、そのまま移せる）。

## 巻き戻し

- [ ] **「leap は時計を動かしたときだけ成立する」という不変条件が破れている**（挙動を変える修正なので要判断）
  - 履歴の底（`CurrentTick == HistoryBegin`）では `Clock.Back()` が失敗するが、**その事実が `ClockNode` から `Player` に伝わっていない**。`Player` から見ると「戻せた押下」と「戻せなかった押下」が区別できず、どちらも解放時に `Leap` になる。
    - 魔素が減るのは `Player.OnActionByClock(BackTick)` の 1 箇所だけで、`Clock.Back()` 成功時しか呼ばれない。底では 1 も減らない。
    - 一方 `Player.ProcessBackButton` の解放側は `ClockOperation` が `StartBack`/`Back`/**`Stop`** なら `Leap` にする。よって tick が 1 も動かないまま leap が成立し、`InvokeClone = true` で `SoraClone` も湧く。
  - **成立には 1 フレームの入力窓が要る**。離した次のフレームで押し直さないと `ProcessBackButton` が `true` を返して `ProcessNormalClock` が走り `Leap → Forward` に落ちる → `ClockNode` が `Tick()` して底から 1 tick 浮き、次の押下では `Back()` が成功して魔素を消費する。押しっぱなしフレームは何枚挟まってもよく、必要なのは押し直しの速さだけ。**人力連打では厳しく、実質連射パッド級**。
  - **害が大きいのは湧く分身がフル尺だから**。分身の寿命は `DenseClone.HistoryEnd - CurrentTick` ＝どれだけ戻したかで決まるので、適当に 1 tick 戻して離しても 1 tick で消える分身しか出ない（設計上ちゃんと自己抑制されている）。ところが底では現在 tick = `maxTick_ - 255` で、しかも `_ProcessLeap` は `record_.Mut` を呼ばないため **`maxTick_` が更新されない**。結果、毎回 255 tick フル尺の分身が湧く。**最初の 1 体は 255 魔素を払って得た正当な分身、2 体目以降がタダ**という構図。
  - 同じ経路で `historyBranches_` のリングを tick を進めずに一周させられる。こうなると古い leap を参照する値の `AdjustTick` が別 leap のスロットを読む（`historyBranches_` が `HistoryLength` 個で足りる根拠がこの不変条件だから）。
  - **推奨**: `ClockNode` が `Clock.Back()` の失敗を `Player` に伝え、「一度も戻れていない押下からの解放では `Leap` にしない」とする。底での押下が完全に空振りになり、1F 窓も `historyBranches_` 一周も同時に閉じる。`Player.ClockAction` に `BackFailed` を足すのが既存の一方向フローに一番素直に乗る。
  - **ただし底での連打を小技として残すかは設計判断**。潰すなら上記、残すなら下項目の定数合わせで入り口だけ塞ぐ。

- [ ] **魔素の上限と巻き戻し可能回数が 1 ずれている**（上の項目と入り口が同じ）
  - 守りたい不変条件は「**魔素はあるのに戻せない**」を起こさないこと。正しくは:
    `SpellGauge.MaxItems <= (Clock.HistoryLength - 1) * numMagicElementsPerTick_`
  - `Clock.Back()` は `CurrentTick > HistoryBegin` のときだけ成功し、`HistoryBegin = CurrentTick - HistoryLength + 1`。したがって**戻せる「回数」は 255 で、バッファのスロット数 256 とは 1 違う**（構造的な理由は [Taiju/Util/Reversible/CLAUDE.md](Taiju/Util/Reversible/CLAUDE.md)）。
  - 現状 `MaxItems = 32 * 8 = 256`、`numMagicElementsPerTick_ = 1` なので `256 <= 255` が破れている。**満タンから底まで戻ると毎回 1 魔素が使えないまま残る**。確率的ではなく再現性のある挙動。
  - `MaxItems` を直接 255 にするのは不可。`MaxItems = SpellGaugeSlot.ItemPerSlot * 8` は UI の 8 スロット構造に直結していて、255 にすると最後のスロットが満タンにならず `SetNumItems` の full 判定と点滅アニメーションが死ぬ。
  - 取りうる案:
    1. **`SpellGaugeSlot.ItemPerSlot` を 31 にする**（`MaxItems = 248 <= 255`）。UI の 8 スロット構造を保ったまま条件を満たす。ゲージの粒度がわずかに粗くなるだけ。
    2. **`Clock.HistoryLength` を 257 にする**（`256 <= 256`）。履歴系は全て `%` 演算で 2 の冪を前提にしていないので動く。ただし「4 秒ちょっと」の仕様に半端な値が入る。
    3. **上項目の不変条件側で直す**。余りの 1 魔素は「戻し始めるには足りない端数」として残るが、無料 leap 連打は同時に閉じる。
  - 1・2 は入り口（底に魔素を残して到達できること）を塞ぐだけで、「`Back()` の失敗と leap の成立が独立している」という構造は残る。`HistoryLength` を変えた瞬間にまた開閉するのはそのため。

- [ ] `Player.OnDamageBySora` の `// TODO: GAME OVER` が未実装。魔素 0 での被弾＝即ゲームオーバー（残機なし）の処理を入れる。
- [ ] **演出（Trigger 由来のイベント）をどう巻き戻すか**が未解決。ボス出現・BGM 切り替え・カメラワークは `Dense`/`Sparse` に素直に載らない。
- [ ] 巻き戻しを多用すると当たり判定が時折壊れる（Godot 物理側の問題）。下の「当たり判定の自前化」で解消する見込み。
- [ ] **`MetaArray<T>` が初期値を捨てている**（唯一のビルド警告 CS9113: パラメーター 'initial' は未読です）
  - `MetaArray<T>(Clock clock, uint size, in T initial)` は `values_ = new T[size]` を作るだけで **`initial` をどこにも書いていない**。常にゼロ埋めで始まる。`Meta<T>` の方は `private T value_ = initial;` で正しく使っている。
  - `readonly struct` のプライマリコンストラクタ + フィールド初期化子では `Array.Fill` を挟めないので、直すなら明示的なコンストラクタにする必要がある。放置されているのはたぶんこれが理由。
  - **ただし `MetaArray<T>` も `Meta<T>` も現状どこからも使われていない**（grep 済み）。`Meta<T>` は「巻き戻さない値を明示する」ための型としてルート [CLAUDE.md](CLAUDE.md) に載せてあるので、API としては残す意図があるはず。
  - 判断: 直す（明示コンストラクタ + `Array.Fill`）か、使う当てが無いなら消す。**どちらにせよ警告は消しておきたい**。今は 1 件しか出ていないので、新しい警告が埋もれない状態を保つ価値がある。
- [ ] **乱数を決定的にする**（`(tick, leap, オブジェクト ID)` から導出する）
  - `Objects/Effect/MagicElementItem.cs` の `RandomNumberGenerator rand_ = new()`（`_Ready` で色と初速を決めている）と `Objects/Effect/Arrow.cs` の `Random.Shared`（色相）がシード無し。
  - 現状は生成時に 1 回引くだけで結果を `Dense` に記録しているので巻き戻し自体は壊れないが、**「キーフレーム＋決定的な再シミュレーション」で履歴バッファを伸ばす道を塞いでいる**。リプレイ機能を入れる場合も同じ問題になる。
  - 安いうちに潰しておくと将来の選択肢が広がる。

## 当たり判定 / 物理

- [ ] **Godot の物理を捨てて当たり判定を自前実装する**（十分に速ければ）
  - 現状 `RigidBody3D` は当たり判定のためだけに使っていて、速度は毎フレーム `_IntegrateForces` で記録から上書きしているので、シミュレーションとしては使っていない。
  - `CollisionShape3D` のノード形状の制約に設計が引きずられている。
  - 巻き戻し時に判定がバグる問題も、自前化すれば tick と同期して決定的に解ける。
  - 2D 平面（Z=0）上の矩形/円判定で足りるはず。

## ステージエディタ

- [ ] **タイムライン型エディタにする**
  - やりたいことは「背景の動きと音楽の様子を UI から一気に確認して操作する」こと。例: 曲が 120 秒目で曲調が変わるなら、その位置がエディタ上で見えて、そこに合わせて敵を置ける。
  - 現状の X 座標はもともと**発火タイミングを決めているだけ**（湧く場所は Rush ノードからの相対座標）なので、X と時間の間に本質的な違いはない。単位の付け替えの問題。
  - 背景スクロールは既に `Stager.StageSpeed` と独立（`Forest` / `Cloud` が各自の `integrateTime` で動く）なので、**背景速度をタイミングで変える演出を入れても敵の湧きには影響しない**。速度を区分定数にして解析的に積分すれば `f(t)` のまま書ける。
  - 音楽トラック・背景トラック・敵配置トラックを 1 画面に並べるのが目標像。
- [ ] `Scenes/Base/trigger.gd` は `trigger_name` を export しているが `stage_compiler.gd` は `node.type` を読んでおり不整合。Trigger を使い始める前に直す。
- [ ] `Trigger` を受けてアドホックな演出スクリプトを起動する仕組み（`Stage01/Stager.OnTrigger` が `NotImplementedException`）。

## 魔女たち

- [ ] もみじの「光の矢」(`Objects/Effect/Arrow.cs`) はホーミング・`ReversibleTubeTrail` 描画まで完成済みだが、発射口の `Sora.InvokeArrow()` が**どこからも呼ばれていない**（意図通り。発動パスを作っていないだけで、デバッグ時に一時的に有効化して確認する用）。導線を決める。
- [ ] かえで（波動砲）は未実装。
- [ ] スペル（`spell` ボタン）は**ちとせの当たり判定消し（`shape_.Disabled = true`）のみ実装済み**。
- [ ] 3 人の登場のさせ方が未決定。**撃墜されるとその魔法が使えなくなり、巻き戻せば魔素と引き換えに復活する**という方向性だけ決まっている。現状 `Sora.Record.Witch` に `Chitose`/`Momiji`/`Kaede` の生存フラグがあるだけ。
- [ ] ちとせ・かえでの 3D モデルがない（もみじのみ存在）。
- [ ] `SoraClone` の `CloneType`（もみじ / かえで）が見た目・弾ともに未分化。

## 環境まわり

- [ ] **`ResourceManager` が `Arrow.tscn` だけ先読みキャッシュを作らない回避策の原因究明**（`ResourceManager.Add` のパス直書き比較）
  - キャッシュを作ると終了時（× ボタン）に必ず `WARNING: 1 ObjectDB instance was leaked at exit` が出る。**原因は未特定**。
  - 有力な仮説（コードからの推論のみ。**未検証**）: `ReversibleTubeTrail` の `private MeshInstance3D meshInstance_ = new();` はフィールド初期化子なので **`Instantiate()` の時点で親なしの Node を 1 個作る**。`AddChild` するのは `_Ready()` の中なので、ツリーに入らないキャッシュでは `_Ready()` が走らず孤児のまま残り、`Arrow` 本体を `Free()` しても子ではないので巻き添えにならない。
    - 傍証: 漏れる数がちょうど 1 個。`arrayMesh_` は Resource（RefCounted）なので自動解放され、Node である `meshInstance_` だけが残る計算と合う。
    - 傍証: リポジトリ全体でフィールド初期化子で Node を作っているのは `ReversibleTubeTrail` だけ（他の Reversible ラッパーは `_Ready()` 内で `new` している）。その派生は `Arrow` だけで、除外対象が 1 つなのと符合する。
  - **検証方法**: 条件を外して起動・終了し、警告が出ることを確認。次に `meshInstance_` の生成を `_Ready()` に移して再確認する。
  - 仮説が当たりなら Godot のバグではなくこちらのライフサイクルの問題なので、`_Ready()` 生成に寄せるか `_Notification(NotificationPredelete)` で自前解放すれば直り、**`Arrow` も他と同じくキャッシュできるようになる**（現状は毎回その場で `Instantiate` するのでスパイク要因になっている）。
  - 現状の回避策はパス直書きなので、**`Arrow.tscn` を移動・改名すると黙って無効化される**点にも注意。
- [ ] `project.godot` の `witch_change` 入力アクションは定義済みだがコードから参照されていない。入力は現状ゲームパッド専用（移動キーのみキーボード）。
- [ ] `project.godot` の `movie_writer/movie_file` が別マシンの絶対パス（`C:/Users/kaede/...`）を指したまま。
