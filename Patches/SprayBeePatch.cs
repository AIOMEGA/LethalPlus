using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using LethalPlus;
using BepInEx.Logging;
using UnityEditor;
using GameNetcodeStuff;
using System.Collections;
using Unity.Netcode;
using UnityEngine.AI;
using System.Runtime.CompilerServices;
using LethalLib.Modules;
using LC_API;
using UnityEngine.PlayerLoop;
//using static LethalPlus.Patches.SprayPaintItemPatch;

namespace LethalPlus.Patches
{
    // Determine if the bee hive is being sprayed
    [HarmonyPatch(typeof(SprayPaintItem))]
    internal class SprayPaintItemPatch
    {
        private static FieldInfo SprayHit = typeof(SprayPaintItem).GetField("sprayHit", BindingFlags.Instance | BindingFlags.NonPublic);
        public static RaycastHit ItemSprayed { get; private set; }
        public static PlayerControllerB PlayerSpraying { get; private set; }
        public static bool IsBeeHiveSprayed { get; set; }
        [HarmonyPatch("SprayPaintClientRpc")]
        [HarmonyPostfix]
        private static void SprayHivePatch(SprayPaintItem __instance, Vector3 sprayPos, Vector3 sprayRot)
        {
            if (__instance != null)
            {
                RaycastHit val = (RaycastHit)SprayHit.GetValue(__instance);
                if (val.collider != null)
                {
                    if (val.collider?.name == "RedLocustHive(Clone)")
                    {
                        IsBeeHiveSprayed = true;
                        //SprayBeePatch.sprayTimer = 0f; // Reset the timer
                        Main.Log.LogInfo("Sprayed");
                    }
                    ItemSprayed = val;
                }
                PlayerSpraying = __instance.playerHeldBy;
                Main.Log.LogInfo("The player spraying is: " + PlayerSpraying?.playerUsername);
            }
        }
    }

    // Find a connection between the bee hive and the closest RedLocustBees

    [HarmonyPatch(typeof(RedLocustBees))]
    internal class OnGameStartedPatch
    {
        public static RedLocustBees[] Bees { get; set; }

        [HarmonyPatch("Start")]
        [HarmonyPostfix]
        static void BeeInstancePatch()
        {
            Bees = UnityEngine.Object.FindObjectsOfType<RedLocustBees>();
        }
    }

    //GameObject.Find(BeeController)
    [HarmonyPatch(typeof(RedLocustBees))]
    internal class SprayBeePatch
    {
        private static float sprayDuration = 25f; // Set the duration in seconds
        public static float sprayTimer;
        [HarmonyPatch("Update")]
        [HarmonyPostfix]
        static void SprayBeePatcher(RedLocustBees __instance)
        {
            if (OnGameStartedPatch.Bees == null)
            {
                Main.Log.LogInfo("bees null");
            }
            if (OnGameStartedPatch.Bees.Length == 0)
            {
                Main.Log.LogInfo("no bees");
            }
            foreach (RedLocustBees instance in OnGameStartedPatch.Bees)
            {
                // Check if the sprayed item is a grabbable object
                GrabbableObject sprayedItem = SprayPaintItemPatch.ItemSprayed.transform?.gameObject?.GetComponent<GrabbableObject>();
                if (sprayedItem != null && sprayedItem == instance.hive)
                {
                    //make docile for 15s

                    if (SprayPaintItemPatch.IsBeeHiveSprayed)
                    {
                        // Modify the behavior of the bees
                        instance.agent.speed = 1f;
                        instance.SwitchToBehaviourState(0);

                        Main.Log.LogInfo("Bees Discombobulated");

                        // Update the timer
                        sprayTimer += Time.deltaTime;

                        // Check if the spray effect duration has elapsed
                        if (sprayTimer >= sprayDuration)
                        {
                            // Reset the flags and timer
                            SprayPaintItemPatch.IsBeeHiveSprayed = false;
                            sprayTimer = 0f;
                            Main.Log.LogInfo("Bees Recovered");
                        }
                        //instance.agent.speed = 0f;

                    }
                }
            }
        }
    }

    // Old way of checking if someone was close and who it was
    /*// Check for players in the vicinity of the hive
    Collider[] playersInVicinity = Physics.OverlapSphere(instance.hive.transform.position, instance.defenseDistance, StartOfRound.Instance.playersMask, QueryTriggerInteraction.Collide);

    // Iterate through players in the vicinity
    foreach (Collider playerCollider in playersInVicinity)
    {
        PlayerControllerB playerControllerB = playerCollider.gameObject.GetComponent<PlayerControllerB>();

        // Check if the player is valid
        if (playerControllerB != null && Vector3.Distance(playerControllerB.transform.position, instance.hive.transform.position) < instance.defenseDistance + 10f)
        {
            // Check if the player sprayed the hive
            if (playerControllerB.playerUsername == SprayPaintItemPatch.PlayerSpraying?.playerUsername)
            {

            }
        }
    }*/
}