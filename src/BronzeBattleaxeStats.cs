using Jotunn.Configs;

namespace BronzeBattleaxe
{
    /// <summary>
    /// Bronze Battleaxe balance. The numbers follow the vanilla Bronze -> Iron gap so the
    /// battleaxe sits in the same relative spot as the other bronze weapons:
    ///
    ///                       Bronze Axe   Iron Axe    Bronze Battleaxe   Iron Battleaxe
    ///   damage (total)      40+40        60+50       50+30              70+40  (slash+chop)
    ///   durability          125          175         150                200
    ///   block power         12           21          16                 28
    ///   attack stamina      8            10          x0.8               x1.0
    ///   tool tier           2            3           2                  3
    ///   forge level         1            2           2                  2
    ///
    /// Per-quality scaling (damage, durability) is identical between bronze and iron in vanilla,
    /// so it is left as inherited from the iron Battleaxe. Movement penalty is -5% (iron: -15%).
    /// Swing speed scales with Axes skill, see SwingSpeed.
    /// </summary>
    internal static class BronzeBattleaxeStats
    {
        public const float Slash = 50f;
        public const float Chop = 30f;
        public const float MaxDurability = 150f;
        public const float BlockPower = 16f;
        public const float AttackForce = 60f;
        public const float MovementModifier = -0.05f;
        public const float StaminaMultiplier = 0.8f;
        public const int ToolTier = 2;
        public const int StationLevel = 2;

        /// <summary>
        /// Vanilla: 30 Ancient Bark (+5), 35 Iron (+15), 4 Leather Scraps. Bronze axes cost ~40%
        /// of their iron counterparts, so the metal drops to 14 Bronze. Fine Wood replaces the
        /// Ancient Bark; it is available at the same forge level as the iron battleaxe.
        /// </summary>
        public static readonly RequirementConfig[] Requirements =
        {
            new RequirementConfig("FineWood", 20, 3, false),
            new RequirementConfig("Bronze", 14, 7, false),
            new RequirementConfig("LeatherScraps", 4, 0, false),
            // Same shape as every vanilla weapon recipe: one upgrader item, one tier below the iron one.
            new RequirementConfig(UpgraderItem, 1, 0, false),
        };

        private const string UpgraderItem = "Upgrader1Weapon";

        public static void Apply(ItemDrop.ItemData.SharedData shared)
        {
            shared.m_damages.m_slash = Slash;
            shared.m_damages.m_chop = Chop;
            shared.m_maxDurability = MaxDurability;
            shared.m_blockPower = BlockPower;
            shared.m_attackForce = AttackForce;
            shared.m_movementModifier = MovementModifier;
            shared.m_toolTier = ToolTier;

            shared.m_attack.m_attackStamina *= StaminaMultiplier;
            shared.m_secondaryAttack.m_attackStamina *= StaminaMultiplier;
        }

        public static void FlagUpgraderResource(Recipe recipe)
        {
            if (recipe == null)
            {
                return;
            }

            foreach (var requirement in recipe.m_resources)
            {
                // Jötunn's unresolved placeholder is named "JVLmock_<prefab>".
                if (requirement.m_resItem != null && requirement.m_resItem.name.EndsWith(UpgraderItem))
                {
                    requirement.m_upgraderResource = true;
                }
            }
        }
    }
}
