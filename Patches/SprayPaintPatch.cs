using HarmonyLib;
using System.Reflection;
using UnityEngine;
using GameNetcodeStuff;
using System;
using System.Linq;

namespace LethalPlus.Patches
{
    // Classes below are for the Spray Paint item
    // These Patches give the Spray Paint 2 new abilities
    // The first ability is to stun Bees if their beehive is sprayed
    // The second is to stun enemies if their mesh is sprayed
    [HarmonyPatch(typeof(SprayPaintItem))]
    internal class SprayPaintItemPatch
    {
        private static FieldInfo SprayHit = typeof(SprayPaintItem).GetField("sprayHit", BindingFlags.Instance | BindingFlags.NonPublic);
        public static RaycastHit ItemSprayed { get; private set; } // Used to store what was sprayed, but only if it sprayed a beehive or enemy
        public static PlayerControllerB PlayerSpraying { get; private set; }
        public static bool IsBeeHiveSprayed { get; set; }
        public static bool IsEnemySprayed { get; set; }
        public static RedLocustBees[] Bees { get; set; } // Used to check and store all Bees in game
        public static EnemyAI[] Enemies { get; set; } // Used to check and store all Enemies in game
        public static EnemyAI Enem { get; set; } // Enem stores the closest enemy to where the Spray Paint was used
        public static SandSpiderAI[] Spiders { get; set; } // Used to check and store all Spiders in game
        public static CrawlerAI[] Crawlers { get; set; } // Used to check and store all Crawlers in game
        public static CentipedeAI[] Centipedes { get; set; } // Used to check and store all Centipedes in game
        public static HoarderBugAI[] Bugs { get; set; } // Used to check and store all HoarderBugs in the game

        [HarmonyPatch("SprayPaintClientRpc")]
        [HarmonyPostfix]
        private static void SprayPatch(SprayPaintItem __instance, Vector3 sprayPos, Vector3 sprayRot)
        {
            if (__instance != null)
            {
                // Get the item that the Spray Paint collided with
                RaycastHit val = (RaycastHit)SprayHit.GetValue(__instance);
                Main.Log.LogInfo("Raycast: " + val);
                Main.Log.LogInfo("Raycast collider: " + val.collider);
                Main.Log.LogInfo("Raycast collider name: " + val.collider?.name);

                // If the Spray Paint collided with a BeeHive
                if (val.collider?.name == "RedLocustHive(Clone)")
                {
                    IsBeeHiveSprayed = true;
                    ItemSprayed = val;
                    Main.Log.LogInfo("Sprayed");
                }
                // If the Spray Paint collided with an Enemy (they have multiple mesh names for some reason)
                else if (val.collider?.name == "Mesh" || val.collider?.name == "Mesh (1)" || val.collider?.name == "CollisionMesh"
                    || val.collider?.name == "AnomalySpawnBox (1)" || val.collider?.name == "AnomalySpawnBox")
                {
                    IsEnemySprayed = true;
                    ItemSprayed = val;
                    Main.Log.LogInfo("Sprayed");
                }

                PlayerSpraying = __instance.playerHeldBy;
                // Scan for all Bees, Enemies and individual Enemy Types and store them in their respective arrays
                Bees = UnityEngine.Object.FindObjectsOfType<RedLocustBees>();
                Enemies = UnityEngine.Object.FindObjectsOfType<EnemyAI>();
                Spiders = UnityEngine.Object.FindObjectsOfType<SandSpiderAI>();
                Crawlers = UnityEngine.Object.FindObjectsOfType<CrawlerAI>();
                Bugs = UnityEngine.Object.FindObjectsOfType<HoarderBugAI>();
                Centipedes = UnityEngine.Object.FindObjectsOfType<CentipedeAI>();

                Main.Log.LogInfo("Spray Paint Pos and Rot: " + sprayPos + " , " + sprayRot); // coordinates of where it was sprayed and what direction

                Enem = GetClosestEnemy(sprayPos); // Scan for closest enemy to where the spray collided with an object

                if (Enem != null)
                {
                    Main.Log.LogInfo("Enemy Returned from Scan: " + Enem.name + "Position: " + sprayPos);
                }
                else
                {
                    Main.Log.LogInfo("Enemy Returned from Scan was null ");
                }
            }
        }

        private static EnemyAI GetClosestEnemy(Vector3 position)
        {
            var enemyLayer = LayerMask.GetMask("Enemies");
            var enemies = Physics.OverlapSphere(position, 50, enemyLayer, QueryTriggerInteraction.Collide)
                                  .Select(collider => collider.GetComponentInParent<EnemyAI>())
                                  .Where(enemy => enemy != null)
                                  .OrderBy(enemy => Vector3.Distance(position, enemy.transform.position))
                                  .FirstOrDefault();
            Main.Log.LogInfo("Assigned ClosestEnemy");
            return enemies;
        }

        /* old version of the GetClosestEnemy
         * public static EnemyAI ScanClosestEnemyToSpray(Vector3 sprayPos) 
        {
            EnemyAI enemyAI = null;
            // Get every existing layer mask that corresponds to an enemy and check a 50 coordinate radius around where
            // the Spray Paint was used to scan for any enemies within that sphere
            var mask = LayerMask.GetMask("Enemies");
            Collider[] EnemyInVicinity = Physics.OverlapSphere(sprayPos, 50, mask, QueryTriggerInteraction.Collide);
            Main.Log.LogInfo("EnemyInVicinity: " + EnemyInVicinity.Length);
            // If no enemy was detected return
            if (!IsEnemySprayed)
            {
                Main.Log.LogInfo("Enemy isn't sprayed");
                return enemyAI;
            }
            // If an Enemy was detected then go through the list of colliders
            foreach (Collider enemyCollider in EnemyInVicinity)
            {
                // Extract the Enemy from its collider
                EnemyAI enemy = enemyCollider.gameObject.GetComponentInParent<EnemyAI>();
                if (enemy != null) 
                {
                    Main.Log.LogInfo("Enemy: " + enemy);
                    Main.Log.LogInfo("Enemy location: " + enemy.transform.position);
                    Main.Log.LogInfo("Spray Position: " + sprayPos);
                    // If the Enemy was within a 5 coordinate distance from where the Spray Paint was used then return it as closest.
                    if (enemy != null && Vector3.Distance(sprayPos, enemy.transform.position) < 5)
                    {
                        enemyAI = enemy;
                        Main.Log.LogInfo("Assigned ClosestEnemy");
                    }
                }
            }
            return enemyAI;
        }*/
    }

    // Class exclusively handles Spraying Bees
    [HarmonyPatch(typeof(RedLocustBees))]
    internal class SprayBeePatch
    {
        private static float sprayDuration = 8f; // Set the duration of stun in seconds
        public static float sprayTimer;
        public static RedLocustBees targetBee;

        [HarmonyPatch("Update")]
        [HarmonyPostfix]
        static void SprayBeePatcher(RedLocustBees __instance)
        {
            GrabbableObject sprayedItem = null;
            // If no bees or if no item was sprayed or if what was sprayed wasn't a hive then return
            if (SprayPaintItemPatch.Bees == null || SprayPaintItemPatch.Bees.Length == 0 
                || SprayPaintItemPatch.ItemSprayed.collider == null || !SprayPaintItemPatch.IsBeeHiveSprayed)
            {
                return;
            }

            // Get the Grabbable GameObject of what was sprayed aka the hive
            sprayedItem = SprayPaintItemPatch.ItemSprayed.transform?.gameObject?.GetComponent<GrabbableObject>();

            bool beesAffected = false;
            // Go through the list of Bees
            foreach (RedLocustBees instance in SprayPaintItemPatch.Bees)
            {
                // If the sprayed item matches the individual instance of bees hive
                if (sprayedItem != null && sprayedItem == instance.hive)
                {
                    // Grab the specifc instance of Bee
                    targetBee = instance;
                    // Modify the behavior of the bee making it docile
                    instance.agent.speed = 0f;
                    instance.SwitchToBehaviourState(0);
                    beesAffected = true;
                    BeeOuchPatch.isStunned = true;
                    //Main.Log.LogInfo("Bees Discombobulated");
                }
            }

            // If the bees have been made docile and the instance of Bee currently being updated in this method matches the one found prior
            if (beesAffected && __instance == targetBee)
            {
                // Update the timer outside of the foreach loop
                sprayTimer += Time.deltaTime;

                // Check if the spray effect duration has elapsed
                if (sprayTimer >= sprayDuration)
                {
                    // Reset the flags and timer
                    BeeOuchPatch.isStunned = false;
                    SprayPaintItemPatch.IsBeeHiveSprayed = false;
                    sprayTimer = 0f;
                    Main.Log.LogInfo("Bees Recovered");
                }
            }
        }
    }

    [HarmonyPatch(typeof(RedLocustBees))]
    internal class BeeOuchPatch
    {
        public static bool isStunned;

        [HarmonyPatch("OnCollideWithPlayer")]
        [HarmonyPrefix]
        static bool BeeOutchPatch(RedLocustBees __instance)
        {
            if (isStunned && __instance == SprayBeePatch.targetBee)
            {
                Main.Log.LogInfo("DON'T HURT HIM!");
                return false;
            }
            return true;
        }
    }

    // Old way of checking if someone was close and who it was // saved this thinking it could be useful and ended up needing it lol
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

    // Below gives the Spray Paint the ability to stun Enemies as long as they are either a Spider, Crawler, HoarderBug or Centipede
    [HarmonyPatch(typeof(EnemyAI))]
    internal class SprayEnemyPatch
    {
        private static float sprayDuration = 8f; // stun duration specific for Enemies
        private static float sprayTimer;
        public static EnemyAI targetEnemy;
        private static bool enemiesAffected;

        [HarmonyPatch("Update")]
        [HarmonyPostfix]
        static void SprayEnemyPatcher(EnemyAI __instance)
        {
            // If no enemy is close or no enemies are spawned or no enemy has been sprayed then return
            if (SprayPaintItemPatch.Enem == null ||
                SprayPaintItemPatch.Enemies.Length == 0 ||
                !SprayPaintItemPatch.IsEnemySprayed)
            {
                return;
            }
            
            //Main.Log.LogInfo($"Processing enemy: {SprayPaintItemPatch.Enem.name}");

            // go through and see what type of enemy it is and check if it matches the closest enemy to save performance
            switch (SprayPaintItemPatch.Enem.name)
            {
                case "SandSpider(Clone)":
                    enemiesAffected |= ProcessSpider(SprayPaintItemPatch.Spiders);
                    break;
                case "Centipede(Clone)":
                    enemiesAffected |= ProcessEnemyType(SprayPaintItemPatch.Centipedes);
                    break;
                case "Crawler(Clone)":
                    enemiesAffected |= ProcessEnemyType(SprayPaintItemPatch.Crawlers);
                    break;
                case "HoarderBug(Clone)":
                    enemiesAffected |= ProcessEnemyType(SprayPaintItemPatch.Bugs);
                    break;
                /*case "SpringMan(Clone)":
                    SpringSprayPatch.isBlind = true;
                    break;*/
                default:
                    //Main.Log.LogInfo("Unknown enemy type.");
                    return;
            }

            // Update the timer and check duration outside the foreach loop
            // Same as bees nothing special here
            if (enemiesAffected && __instance == SprayPaintItemPatch.Enem)
            {
                sprayTimer += Time.deltaTime;

                if (sprayTimer >= sprayDuration)
                {
                    ResetSprayEffect();
                }
            }
        }

        // Below handles stunning the inputted enemy
        private static bool ProcessEnemyType<T>(T[] enemies) where T : EnemyAI
        {
            foreach (var enemy in enemies)
            {
                if (enemy != null &&
                    SprayPaintItemPatch.Enem.transform.position == enemy.transform.position)
                {
                    enemy.agent.speed = 0f;
                    targetEnemy = enemy;
                    //Main.Log.LogInfo($"{enemy} Discombobulated");

                    return true;
                }
            }
            return false;
        }
        // Spiders for some reason arent affected when passed through the previous one so I had to make it's own dedicated method to extract its
        // SandSpiderAI and use variables that belong to its base class to stun it
        private static bool ProcessSpider<T>(T[] enemies) where T : EnemyAI
        {
            foreach (SandSpiderAI spider in SprayPaintItemPatch.Spiders)
            {
                if (spider != null
                    && SprayPaintItemPatch.Enem.transform.position != null
                    && spider.transform.position != null
                    && SprayPaintItemPatch.Enem.transform.position == spider.transform.position
                    && SprayPaintItemPatch.IsEnemySprayed == true)
                {
                    spider.agent.speed = 0f;
                    spider.spiderSpeed = 0f;
                    //Main.Log.LogInfo($"{spider} Discombobulated");
                    spider.creatureAnimator.SetBool("moving", true);
                    return true;
                }
            }
            return false;
        }

        private static void ResetSprayEffect()
        {
            SprayPaintItemPatch.IsEnemySprayed = false;
            sprayTimer = 0f;
            enemiesAffected = false;
            Main.Log.LogInfo($"{SprayPaintItemPatch.Enem} recovered.");
        }
    }

    /*[HarmonyPatch(typeof(SpringManAI))]
    class SpringSprayPatch
    {
        public static bool isBlind = false;
        [HarmonyPatch("Update")]
        [HarmonyPostfix]
        static void SpringManBlindPatch(SpringManAI __instance)
        {
            Main.Log.LogInfo($"SpringMan {isBlind}");
            if (isBlind)
            {
                Main.Log.LogInfo($"Speedify");
                __instance.agent.speed = 0f;
            }
        }
    }*/

    /*[HarmonyPatch(typeof(SpringManAI), "HasLineOfSightToPosition")]
    class SpringManAI_HasLineOfSightToPosition_Patch
    {
        static bool Prefix(SpringManAI __instance, Vector3 pos, ref bool __result)
        {
            if (isBliind)
            {
                // If the AI is blind, return true without executing the original method
                __result = true;
                return false; // Skip the original method
            }

            // Continue with the original method execution
            return true;
        }
    }*/

}