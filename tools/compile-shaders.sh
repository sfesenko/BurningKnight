#!/bin/sh
# Rebuilds the eight compiled shaders — the only content that still needs MGCB, because .fx must
# be compiled to bytecode and on Linux that runs through Wine. Everything else loads raw.
set -e

ROOT="$(cd "$(dirname "$0")/.." && pwd)"

: "${MGFXC_WINE_PATH:=$HOME/.winemonogame}"
export MGFXC_WINE_PATH

if ! command -v dotnet >/dev/null 2>&1; then
	echo "needs the .NET SDK and the MGCB tool: dotnet tool install -g dotnet-mgcb" >&2
	exit 1
fi

if [ ! -d "$MGFXC_WINE_PATH" ]; then
	echo "MGFXC_WINE_PATH is not a directory: $MGFXC_WINE_PATH" >&2
	echo "point it at a Wine prefix with the MonoGame effect compiler (see the README)" >&2
	exit 1
fi

exec dotnet mgcb "$ROOT/Content/Content.mgcb"
