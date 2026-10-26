#!/usr/bin/env bash
# Builds a Thunderstore package: dist/BronzeBattleaxe-<version>.zip
# Usage: scripts/package.sh            (extra args are passed to `dotnet build`, e.g. -p:ValheimPath=...)
set -euo pipefail

cd "$(dirname "$0")/.."

if ! command -v dotnet >/dev/null 2>&1 && [ -x "$HOME/.dotnet/dotnet" ]; then
    export PATH="$HOME/.dotnet:$PATH" DOTNET_ROOT="$HOME/.dotnet"
fi

# The version has to agree in three places; Jötunn's network check uses the plugin's, Thunderstore the manifest's.
plugin_version=$(sed -n 's/.*PluginVersion = "\([^"]*\)".*/\1/p' src/BronzeBattleaxePlugin.cs)
manifest_version=$(sed -n 's/.*"version_number": *"\([^"]*\)".*/\1/p' thunderstore/manifest.json)
csproj_version=$(sed -n 's/.*<Version>\([^<]*\)<\/Version>.*/\1/p' BronzeBattleaxe.csproj)

if [ -z "$plugin_version" ] || [ "$plugin_version" != "$manifest_version" ] || [ "$plugin_version" != "$csproj_version" ]; then
    echo "Version mismatch: plugin='$plugin_version' manifest='$manifest_version' csproj='$csproj_version'" >&2
    exit 1
fi

if ! grep -q "^## $plugin_version\b" thunderstore/CHANGELOG.md; then
    echo "thunderstore/CHANGELOG.md has no '## $plugin_version' entry" >&2
    exit 1
fi

dotnet build BronzeBattleaxe.csproj -c Release "$@"

name=$(sed -n 's/.*"name": *"\([^"]*\)".*/\1/p' thunderstore/manifest.json)
out="dist/${name}-${plugin_version}.zip"
mkdir -p dist
rm -f "$out"

# Files must sit at the zip root (not inside a folder) or Thunderstore rejects the package.
staging=$(mktemp -d)
trap 'rm -rf "$staging"' EXIT
cp bin/Release/BronzeBattleaxe.dll thunderstore/manifest.json thunderstore/README.md thunderstore/CHANGELOG.md thunderstore/icon.png "$staging/"
(cd "$staging" && zip -q -X "$OLDPWD/$out" BronzeBattleaxe.dll manifest.json README.md CHANGELOG.md icon.png)

echo "Created $out"
unzip -l "$out"
