# Bronze Battleaxe

Adds a **Bronze Battleaxe**: a bronze-tier take on the iron Battleaxe, for when you want a two-handed axe
before you have the iron for one.

It reuses the vanilla Battleaxe's animations, hit effects and sounds, with its own bronze model, textures and
inventory icon.

*Model by [leonsavelind](https://skfb.ly/6uKOp), CC BY 4.0 - see [Credits](#credits).*

## Crafting

Forge level 2 (the same as the iron Battleaxe).

| Ingredient    | Amount | Per upgrade level |
|---------------|--------|-------------------|
| Fine Wood     | 20     | +3                |
| Bronze        | 14     | +7                |
| Leather Scraps| 4      | -                 |

## Stats

Quality 1. The bronze/iron ratios follow the vanilla Bronze Axe / Iron Axe gap.

|                    | Bronze Battleaxe | Battleaxe (iron) |
|--------------------|------------------|------------------|
| Slash              | 50               | 70               |
| Chop               | 30               | 40               |
| Durability         | 150              | 200              |
| Block power        | 16               | 28               |
| Attack force       | 60               | 70               |
| Attack stamina     | x0.8             | x1               |
| Movement penalty   | -5%              | -15%             |
| Tool tier          | 2                | 3                |

Per-quality scaling, weight and deflection are the same as the iron Battleaxe.

### Axes skill and swing speed

The Bronze Battleaxe swings faster the higher your Axes skill: +0% at skill 0, up to +25% at skill 100
(linear in between). Wind-up, hit timing and combo windows all speed up together.

## Config

`BepInEx/config/com.ixillia.bronzebattleaxe.cfg`

- `Combat / MaxSwingSpeedBonus` (default `0.25`): swing speed bonus at Axes skill 100.
  Set to `0` to disable. Admin-only; the server's value applies.

## Multiplayer

The server and every player need the mod installed. Versions must match on the minor number (1.1.x with 1.1.x).

## Console

`spawn BronzeBattleaxe` with devcommands enabled.

## Known limitations

- Only English text is provided; other languages show the English name.
- If the bundled model fails to load, the item falls back to the iron Battleaxe's model recoloured bronze (a warning is logged).

## Credits

The 3D model is based on **"Glass Battleaxe, 1958 Tris, Low Poly"** by [leonsavelind](https://sketchfab.com/leonsavelind)
([source](https://skfb.ly/6uKOp)), licensed under
[Creative Commons Attribution 4.0](http://creativecommons.org/licenses/by/4.0/).
Changes: rotated and scaled to fit Valheim's battleaxe, UVs and normals kept, textures re-coloured from glass to
bronze (albedo, metallic/smoothness), ambient occlusion baked into the albedo.

Built with [Jötunn](https://valheim-modding.github.io/Jotunn/).
