using HarmonyLib;

namespace BronzeBattleaxe
{
    /// <summary>
    /// Speeds up the Bronze Battleaxe's swing animation with the wielder's Axes skill.
    ///
    /// Valheim has no attack-speed stat, but attack timing (hit, trail, chain windows) is driven by the
    /// animator, so scaling the animator speed moves all of it together. The battleaxe clips also set the
    /// animator speed themselves mid-swing (slow wind-up, fast follow-through), so those events are scaled
    /// too instead of being allowed to overwrite the bonus. Outside an attack the game resets the speed to 1.
    ///
    /// Only the owning client scales: it is the one that knows the player's skill, and the owner's animator
    /// speed is synced to everyone else.
    /// </summary>
    internal static class SwingSpeed
    {
        public static float Multiplier(Character character)
        {
            if (!(character is Humanoid humanoid) || !humanoid.IsOwner() || !humanoid.InAttack())
            {
                return 1f;
            }

            return MultiplierForWeapon(humanoid, humanoid.GetCurrentWeapon());
        }

        public static float MultiplierForWeapon(Humanoid humanoid, ItemDrop.ItemData weapon)
        {
            if (weapon == null || weapon.m_shared.m_name != "$" + BronzeBattleaxePlugin.NameToken)
            {
                return 1f;
            }

            return 1f + BronzeBattleaxePlugin.MaxSwingSpeedBonus.Value * humanoid.GetSkillFactor(weapon.m_shared.m_skillType);
        }
    }

    /// <summary>Scale the speed changes made by animation events inside the attack clips.</summary>
    [HarmonyPatch(typeof(CharacterAnimEvent), nameof(CharacterAnimEvent.Speed))]
    internal static class CharacterAnimEvent_Speed_Patch
    {
        private static void Prefix(ref float speedScale, Character ___m_character)
        {
            speedScale *= SwingSpeed.Multiplier(___m_character);
        }
    }

    /// <summary>Apply the bonus from the first frame, before the clip's own speed events fire.</summary>
    [HarmonyPatch(typeof(Attack), nameof(Attack.Start))]
    internal static class Attack_Start_Patch
    {
        private static void Postfix(bool __result, Humanoid character, ZSyncAnimation zanim, ItemDrop.ItemData weapon)
        {
            if (!__result || !character.IsOwner())
            {
                return;
            }

            var multiplier = SwingSpeed.MultiplierForWeapon(character, weapon);
            if (multiplier != 1f)
            {
                zanim.SetSpeed(multiplier);
            }
        }
    }
}
