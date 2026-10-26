using System.Collections.Generic;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using Jotunn;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using Jotunn.Utils;

namespace BronzeBattleaxe
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    [BepInDependency(Main.ModGuid)]
    [NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.Minor)]
    public class BronzeBattleaxePlugin : BaseUnityPlugin
    {
        public const string PluginGuid = "com.ixillia.bronzebattleaxe";
        public const string PluginName = "Bronze Battleaxe";
        public const string PluginVersion = "1.0.0";

        /// <summary>Vanilla prefab we clone: the iron-tier two-handed battleaxe.</summary>
        private const string BasePrefab = "Battleaxe";

        private const string PrefabName = "BronzeBattleaxe";
        internal const string NameToken = "item_bronzebattleaxe";
        private const string DescriptionToken = "item_bronzebattleaxe_description";

        internal static ManualLogSource Log;
        internal static ConfigEntry<float> MaxSwingSpeedBonus;

        private void Awake()
        {
            Log = Logger;

            MaxSwingSpeedBonus = Config.Bind("Combat", "MaxSwingSpeedBonus", 0.25f,
                new ConfigDescription(
                    "Swing speed bonus at Axes skill 100, scaled linearly from 0 at skill 0 (0.25 = 25% faster).",
                    new AcceptableValueRange<float>(0f, 1f),
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));

            new Harmony(PluginGuid).PatchAll(Assembly.GetExecutingAssembly());

            LocalizationManager.Instance.GetLocalization().AddTranslation("English", new Dictionary<string, string>
            {
                { NameToken, "Bronze Battleaxe" },
                { DescriptionToken, "A heavy two-handed axe cast from bronze. Duller than iron, but swings faster as your Axes skill grows." },
            });

            PrefabManager.OnVanillaPrefabsAvailable += AddBronzeBattleaxe;
        }

        private void AddBronzeBattleaxe()
        {
            PrefabManager.OnVanillaPrefabsAvailable -= AddBronzeBattleaxe;

            // Everything not overridden below (attack animations, hit effects, trails, sounds,
            // deflection, weight, movement penalty...) is inherited from the iron Battleaxe.
            var config = new ItemConfig
            {
                Name = "$" + NameToken,
                Description = "$" + DescriptionToken,
                CraftingStation = CraftingStations.Forge,
                MinStationLevel = BronzeBattleaxeStats.StationLevel,
                Requirements = BronzeBattleaxeStats.Requirements,
            };

            var item = new CustomItem(PrefabName, BasePrefab, config);
            if (item.ItemPrefab == null || item.ItemDrop == null)
            {
                Log.LogError($"Could not clone '{BasePrefab}', Bronze Battleaxe was not added");
                return;
            }

            BronzeBattleaxeStats.Apply(item.ItemDrop.m_itemData.m_shared);

            // Jötunn builds the recipe from the config but cannot flag the upgrader ingredient.
            BronzeBattleaxeStats.FlagUpgraderResource(item.Recipe?.Recipe);

            BronzeAppearance.TryApply(item);

            ItemManager.Instance.AddItem(item);
            Log.LogInfo("Added Bronze Battleaxe");
        }
    }
}
