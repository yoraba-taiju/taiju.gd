#!/usr/bin/env bash
# /run-test の実体。GODOT_BIN を解決してから gdUnit4 のテストを走らせる。
# 単体でも動く:  bash .claude/skills/run-test/run-test.sh [テスト名の一部]
set -u

root="$(git rev-parse --show-toplevel 2>/dev/null || true)"
if [ -z "$root" ]; then
  echo "リポジトリの中で実行すること。"
  exit 0
fi
cache="$root/.claude/skills/run-test/godot_bin"

resolve_godot() {
  if [ -n "${GODOT_BIN:-}" ] && [ -f "$GODOT_BIN" ]; then
    printf '%s' "$GODOT_BIN"; return 0
  fi
  if [ -f "$cache" ]; then
    local cached; cached="$(head -1 "$cache")"
    if [ -f "$cached" ]; then printf '%s' "$cached"; return 0; fi
  fi
  local found
  found="$(find "$HOME/opt" "/c/Program Files" "/c/Program Files (x86)" -maxdepth 3 \
             -iname 'Godot*mono*console*.exe' 2>/dev/null | sort | tail -1)"
  if [ -n "$found" ]; then
    printf '%s' "$found" > "$cache"
    printf '%s' "$found"; return 0
  fi
  return 1
}

if ! godot="$(resolve_godot)"; then
  echo "GODOT_BIN が見つからない。"
  echo "mono 版の console 実行ファイル（Godot_v*_mono_*_console.exe）のパスを"
  echo "  $cache"
  echo "に書くか、GODOT_BIN に設定する。"
  exit 0
fi
echo "GODOT_BIN=$godot"

filter=()
if [ $# -gt 0 ] && [ -n "${1:-}" ]; then
  filter=(--filter "FullyQualifiedName~$1")
  echo "filter=FullyQualifiedName~$1"
fi

cd "$root/Taiju" || exit 0
GODOT_BIN="$godot" dotnet test "${filter[@]+"${filter[@]}"}"
echo "--- dotnet test exit=$?"
echo "--- git status:"
git -C "$root" status --porcelain
exit 0
