#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "$0")" && pwd)"
GAME="$ROOT/src/Throughput.Game"

GODOT_MONO="/Applications/Godot_mono.app/Contents/MacOS/Godot"

if [ ! -x "$GODOT_MONO" ]; then
  echo "ERROR: Godot .NET build not found at $GODOT_MONO" >&2
  echo "The plain 'godot' binary cannot run C# projects." >&2
  exit 1
fi

exec "$GODOT_MONO" --path "$GAME" "$@"
