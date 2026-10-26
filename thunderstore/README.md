# Bronze Battleaxe

Adds a **Bronze Battleaxe**: a bronze-tier take on the iron Battleaxe, for when you want a two-handed axe
before you have the iron for one.

It reuses the vanilla Battleaxe's animations, hit effects and sounds. The metal parts of the model and the
inventory icon are tinted bronze.

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

The server and every player need the mod installed. Versions must match on the minor number (0.1.x with 0.1.x).

## Console

`spawn BronzeBattleaxe` with devcommands enabled.

## Known limitations

- Only English text is provided; other languages show the English name.
- Appearance is the iron Battleaxe's model, recoloured at load time, not a bespoke model.
