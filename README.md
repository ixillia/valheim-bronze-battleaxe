# Bronze Battleaxe

A Valheim mod (BepInEx 5 + [Jötunn](https://github.com/Valheim-Modding/Jotunn)) that adds a **Bronze Battleaxe**:
a bronze-tier version of the iron-tier Battleaxe.

It clones the vanilla `Battleaxe` prefab, so animations, hit effects, trails and sounds are inherited;
only the stats, recipe and colours differ.

## Stats

Values for quality 1. Bronze/iron ratios follow the vanilla Bronze Axe / Iron Axe gap
(`src/BronzeBattleaxeStats.cs` has the reasoning).

|                        | Bronze Battleaxe | Battleaxe (iron) |
|------------------------|------------------|------------------|
| Slash                  | 50               | 70               |
| Chop                   | 30               | 40               |
| Durability             | 150              | 200              |
| Block power            | 16               | 28               |
| Attack force           | 60               | 70               |
| Attack stamina         | x0.8             | x1               |
| Movement penalty       | -5%              | -15%             |
| Swing speed            | +0% to +25% with Axes skill (0 to 100) | - |
| Tool tier              | 2                | 3                |
| Per-quality scaling, weight, deflection | same as iron | |

**Recipe** (Forge level 2, same as iron):
20 (+3/level) Fine Wood, 14 (+7/level) Bronze, 4 Leather Scraps. Iron needs 30 (+5) Ancient Bark and 35 (+15) Iron.

The metal parts of the model and the inventory icon are re-coloured bronze at load time.
If that fails the item still works and keeps the iron look (a warning is logged).

## Config

`BepInEx/config/com.ixillia.bronzebattleaxe.cfg` (admin-only, synced from the server):

- `Combat / MaxSwingSpeedBonus` (default `0.25`): swing speed bonus at Axes skill 100, scaled linearly from 0 at skill 0.

## Building

Needs the .NET SDK (8+) and a Valheim install with BepInEx.

```sh
dotnet build -c Release                                  # -> bin/Release/BronzeBattleaxe.dll
dotnet build -c Release -p:DeployToGame=true             # also copies it to <Valheim>/BepInEx/plugins/BronzeBattleaxe
```

The Valheim path defaults to `~/.local/share/Steam/steamapps/common/Valheim`;
override with `-p:ValheimPath=...` or the `VALHEIM_INSTALL` environment variable.

## Thunderstore package

```sh
scripts/package.sh      # -> dist/BronzeBattleaxe-<version>.zip
```

Builds in Release and zips `BronzeBattleaxe.dll` with the files in `thunderstore/` (manifest, README, changelog, icon),
all at the zip root as Thunderstore requires. It refuses to run unless the version matches in
`src/BronzeBattleaxePlugin.cs` (`PluginVersion`), `BronzeBattleaxe.csproj` (`<Version>`) and
`thunderstore/manifest.json`, and `thunderstore/CHANGELOG.md` has an entry for it.
`thunderstore/icon.png` is drawn by `scripts/make_icon.py` (needs Pillow); replace it with your own art if you like.

## Multiplayer

Every player and the server need the mod (`EveryoneMustHaveMod`).
