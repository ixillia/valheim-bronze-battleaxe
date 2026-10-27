#!/usr/bin/env bash
# Builds a Thunderstore package: dist/<name>-<version>.zip from the thunderstore/ folder + the built DLL.
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
out="dist/Ixillia-${name}-${plugin_version}.zip"
mkdir -p dist
rm -f "$out"

# Thunderstore wants manifest/README/icon/CHANGELOG at the zip root (not inside a wrapping folder);
# the DLL goes in a subfolder, which r2modman/Thunderstore Mod Manager install under BepInEx/.
# The built DLL is copied into thunderstore/ (git-ignored) so that folder is the single package layout.
plugin_dir="thunderstore/plugins/Ixillia.BronzeBattleaxe"
mkdir -p "$plugin_dir"
cp bin/Release/BronzeBattleaxe.dll "$plugin_dir/"

# Zip an explicit list, so stray files in thunderstore/ (old zips, .gitkeep) never end up in the package.
(cd thunderstore && zip -q -r -X "../$out" manifest.json README.md CHANGELOG.md icon.png plugins -x '*.gitkeep')

echo "Created $out"
unzip -l "$out"
