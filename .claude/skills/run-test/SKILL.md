---
name: run-test
description: gdUnit4 のテストを走らせる。コードを変えたあと壊れていないか確かめたいとき、ユーザーがテストの実行を求めたとき、特定のテストだけ絞って動かしたいときに使う。GODOT_BIN の解決も含めて全部やる。
argument-hint: [テスト名の一部]
allowed-tools: Bash
---

```!
bash "${CLAUDE_SKILL_DIR}/run-test.sh" $ARGUMENTS
```

上が実行結果。`run-test.sh` が GODOT_BIN の解決からテスト実行までを全部やっているので、判断することは無い。以下だけ守る。

- 結果をそのまま報告する。失敗したテストがあれば、テスト名と出力を省略せずに示す。
- `GODOT_BIN が見つからない` と出ていたら、mono 版の console 実行ファイル（`Godot_v*_mono_*_console.exe`）のパスをユーザーに聞き、メッセージに出ているファイルに書く。次回以降はそれが使い回される。マシン依存なので `.gitignore` してある。
- 末尾の `git status` に身に覚えのない `.cs` の差分が出ていたら報告する。`Taiju/project.godot` の `[editor_overrides]` が効いていないということ。
- **呼び直すとテストも走り直す**（`!` ブロックは読み込みのたびに実行される）。同じ結果をもう一度見たいだけなら呼び直さない。

モデルを経由せずに走らせたいときは、同じスクリプトを直接叩けばよい。

```bash
bash .claude/skills/run-test/run-test.sh [テスト名の一部]
```
