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
        private const string modVersion = "0.7.0";
        
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

            harmony.PatchAll(typeof(Main));
            harmony.PatchAll(typeof(SprayBeePatch));
            harmony.PatchAll(typeof(SprayPaintItemPatch));
            harmony.PatchAll(typeof(SprayEnemyPatch));
            harmony.PatchAll(typeof(BeeOuchPatch));
            harmony.PatchAll(typeof(SprayPricePatch));
            //harmony.PatchAll(typeof(PlayerControllerBPatch));

            Log.LogInfo("Mod Loaded");

            //harmony.PatchAll(typeof(SpringSprayPatch));

            /*Assets.PopulateAssets();
            EnemyType val = Assets.MainAssetBundle.LoadAsset<EnemyType>("SkibidiDef");
            TerminalNode val2 = Assets.MainAssetBundle.LoadAsset<TerminalNode>("SkibidiFile");
            TerminalKeyword val3 = Assets.MainAssetBundle.LoadAsset<TerminalKeyword>("Skibidi");
            NetworkPrefabs.RegisterNetworkPrefab(val.enemyPrefab);
            Type typeFromHandle = typeof(PitFiendAI);

            Enemies.RegisterEnemy(val, 22, (LevelTypes)510, (SpawnType)0, val2, val3);*/

        }

    }

    //gonna use as reference later
    /*public static class Assets
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
    }*/
}
