using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using LethalLib.Modules;
using UnityEngine;
using static LethalLib.Modules.Enemies;
using static LethalLib.Modules.Levels;
using UnityEngine.Assertions;
using System.IO;
using System.Reflection;
using LethalPlus.Patches;
using GameNetcodeStuff;

namespace LethalPlus
{
    [BepInPlugin(modGUID, modName, modVersion)]
    public class Main : BaseUnityPlugin
    {
        private const string modGUID = "OMEGA.LethalPlus";
        private const string modName = "LethalPlus";
        private const string modVersion = "0.1.0";

        private readonly Harmony harmony = new Harmony(modGUID);

        private static Main Instance;

        public static ManualLogSource Log { get; private set; }

        void Awake()
        {
            if (Instance == null) // Allows for the creation of variables when there are many patches
            {
                Instance = this;
            }

            Log = BepInEx.Logging.Logger.CreateLogSource(modGUID);
            Log.LogInfo("Mod Loaded");

            harmony.PatchAll(typeof(Main)); // Add an individual patch for any created aka CrawlerPatch ect.
            harmony.PatchAll(typeof(SprayBeePatch));
            harmony.PatchAll(typeof(SprayPaintItemPatch));
            //harmony.PatchAll(typeof(PlayerControllerBPatch));
            harmony.PatchAll(typeof(OnGameStartedPatch));

            /*Assets.PopulateAssets();
            EnemyType val = Assets.MainAssetBundle.LoadAsset<EnemyType>("SkibidiDef");
            TerminalNode val2 = Assets.MainAssetBundle.LoadAsset<TerminalNode>("SkibidiFile");
            TerminalKeyword val3 = Assets.MainAssetBundle.LoadAsset<TerminalKeyword>("Skibidi");
            NetworkPrefabs.RegisterNetworkPrefab(val.enemyPrefab);
            Type typeFromHandle = typeof(PitFiendAI);

            Enemies.RegisterEnemy(val, 22, (LevelTypes)510, (SpawnType)0, val2, val3);*/

        }


        public static void LogPlayerNames()
        {
            if (StartOfRound.Instance.allPlayerScripts != null)
            {
                foreach (PlayerControllerB playerScript in StartOfRound.Instance.allPlayerScripts)
                {
                    if (playerScript != null)
                    {
                        // Log the player's name
                       Log.LogInfo("Player Name: " + playerScript.playerUsername);
                    }
                    else
                    {
                        Log.LogInfo("Player Name: is null");
                    }
                }
            }
        }
    }

    /*[HarmonyPatch(typeof(StartOfRound))]
    internal class OnGameStartedPatch
    {
        public static RedLocustBees[] Bees { get; set; }

        [HarmonyPatch("Start")]
        [HarmonyPostfix]
        static void BeeInstancePatch()
        {
            Bees = UnityEngine.Object.FindObjectsOfType<RedLocustBees>();
        }
    }*/

    public static class Assets
    {
        public static string mainAssetBundleName = "skibidibundle";

        public static AssetBundle MainAssetBundle = null;

        private static string GetAssemblyName()
        {
            return Assembly.GetExecutingAssembly().FullName.Split(',')[0];
        }

        public static void PopulateAssets()
        {
            if ((UnityEngine.Object)(object)MainAssetBundle == null)
            {
                using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(GetAssemblyName() + "." + mainAssetBundleName))
                {
                    MainAssetBundle = AssetBundle.LoadFromStream(stream);
                }
            }
        }
    }
}
