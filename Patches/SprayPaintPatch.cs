using HarmonyLib;
using System.Reflection;
using UnityEngine;
using GameNetcodeStuff;
using System;
using System.Linq;
using BepInEx;
using BepInEx.Configuration;
using UnityEngine.AI;
using System.Collections;

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
                    SprayBeePatch.sprayTimer = 0f;
                    Main.Log.LogInfo("Sprayed");
                }
                // If the Spray Paint collided with an Enemy (they have multiple mesh names for some reason)
                else if (val.collider?.name == "Mesh" || val.collider?.name == "Mesh (1)" || val.collider?.name == "CollisionMesh"
                    || val.collider?.name == "AnomalySpawnBox (1)" || val.collider?.name == "AnomalySpawnBox")
                {
                    IsEnemySprayed = true;
                    ItemSprayed = val;
                    SprayEnemyPatch.sprayTimer = 0f;
                    SprayEnemyPatch.processTimer = 0f;
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

                SprayEnemyPatch.sprayDuration = UnityEngine.Random.Range(8f, 20f);
                Main.Log.LogInfo($"Enemy Stun Duration: {SprayEnemyPatch.sprayDuration}");

                SprayBeePatch.sprayDuration = UnityEngine.Random.Range(3f, 8f);
                Main.Log.LogInfo($"Bee Stun Duration: {SprayBeePatch.sprayDuration}");

                SprayEnemyPatch.behaviorState = UnityEngine.Random.Range(1, 4);
                Main.Log.LogInfo($"Behavior: {SprayEnemyPatch.behaviorState}");

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
        public static float sprayDuration; // Set the duration of stun in seconds
        public static float sprayTimer = 0f;
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
        public static float sprayDuration;
        public static float sprayTimer = 0f;
        private static bool enemiesAffected;
        public static int behaviorState;
        public static PlayerControllerB closestPlayer;
        private static float processTime = 1f;
        public static float processTimer = 0f;
        private static Vector3 flightDestination;
        //private static Vector3 oldDestination;
        private static bool canRun = false;
        private static bool isEligible = false;
        private static SandSpiderAI targetSpider;
        private static bool setDest = false;

        [HarmonyPatch("Update")]
        [HarmonyPostfix]
        static void SprayEnemyPatcher(EnemyAI __instance)
        {
            if (!ShouldProcess()) { return; }
            // go through and see what type of enemy it is and check if it matches the closest enemy to save performance
            switch (SprayPaintItemPatch.Enem.name)
            {
                case "SandSpider(Clone)":
                    enemiesAffected |= ProcessSpider(__instance);
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
            if (enemiesAffected && __instance == SprayPaintItemPatch.Enem)
            {
                sprayTimer += Time.deltaTime;

                if (sprayTimer >= sprayDuration)
                {
                    ResetSprayEffect();
                }
            }
        }

        private static bool ShouldProcess()
        {
            return SprayPaintItemPatch.Enem != null &&
                   SprayPaintItemPatch.Enemies.Length > 0 &&
                   SprayPaintItemPatch.IsEnemySprayed;
        }

        private static bool ProcessSpider(EnemyAI instance)
        {
            foreach (SandSpiderAI spider in SprayPaintItemPatch.Spiders)
            {
                if (IsEligibleSpider(spider))
                {
                    targetSpider = spider;
                    isEligible = true;
                }
            }
            if (isEligible)
            {
                ProcessSpiderBehavior(targetSpider);
            }
            if (isEligible && instance == SprayPaintItemPatch.Enem && targetSpider.transform.position == instance.transform.position)
            {
                processTimer += Time.deltaTime;
                return true;
            }
            else
            {
                return false;
            }
        }
        private static bool IsEligibleSpider(SandSpiderAI spider)
        {
            return spider != null &&
                   SprayPaintItemPatch.Enem.transform.position == spider.transform.position &&
                   SprayPaintItemPatch.IsEnemySprayed;
        }
        private static void ProcessSpiderBehavior(SandSpiderAI spider)
        {
            closestPlayer = spider.GetClosestPlayer();
            if (closestPlayer == null)
            {
                return;
            }
            else
            {
                Main.Log.LogInfo($"{DateTime.Now.ToString("HH:mm:ss")} player position: {closestPlayer.transform.position}");
            }
            switch (behaviorState)
            {
                case 1: // Fight
                    if (processTimer <= processTime)
                    {
                        spider.spiderSpeed = 0f;
                        spider.agent.speed = 0f;
                    }
                    else
                    {
                        spider.spiderSpeed = 12f;
                        spider.agent.speed = 12f;
                        spider.agent.SetDestination(closestPlayer.transform.position);
                    }
                    Main.Log.LogInfo($"{DateTime.Now.ToString("HH:mm:ss")} {spider} Decided to FIGHT");
                    break;
                case 2: // Flight
                    Main.Log.LogInfo($"{DateTime.Now.ToString("HH:mm:ss")} RS1 Spider info:\n Spider Positon: {spider.transform.position}\n flightDestination: {flightDestination}\n Distance to destination: {Vector3.Distance(spider.transform.position, flightDestination)}\n Destination: {spider.agent.destination}\n Path: {spider.agent.pathEndPosition}");
                    if (processTimer <= processTime) // Spider first sprayed it pauses for a second
                    {
                        Main.Log.LogInfo($"{DateTime.Now.ToString("HH:mm:ss")} processing {processTimer} pause");
                        spider.spiderSpeed = 0f;
                        spider.agent.speed = 0f;
                        DeterminePath(spider); // determines place to run away to

                    }
                    else if (canRun && Vector3.Distance(spider.transform.position, flightDestination) <= 5f) // if spider reaches destination
                    {
                        if (!IsPlayerInPath(spider, flightDestination)) // check if player is in path
                        {
                            DeterminePath(spider); // determine new path
                        }
                        else
                        {
                            Main.Log.LogInfo($"{DateTime.Now.ToString("HH:mm:ss")} {spider} is trapped 3");
                            behaviorState = 1; // Switch to attack
                        }
                    }
                    else if (spider.agent.destination != flightDestination // If the Spider ever tries going somewhere after already getting a path
                        || spider.agent.pathEndPosition != flightDestination && setDest && canRun) // for instance if a player walks into its webbing
                    {
                        Main.Log.LogInfo($"{DateTime.Now.ToString("HH:mm:ss")} Spider destination not FD");
                        NavMeshPath path = new NavMeshPath();
                        NavMesh.CalculatePath(spider.transform.position, flightDestination, NavMesh.AllAreas, path);
                        spider.spiderSpeed = 14f;
                        spider.agent.speed = 14f;
                        // ReSet the path for the spider
                        spider.agent.SetPath(path);
                        Main.Log.LogInfo($"Set Path 2 {spider.agent.pathEndPosition}");
                        if (spider.agent.destination != flightDestination)
                        {
                            Main.Log.LogInfo($"{DateTime.Now.ToString("HH:mm:ss")} Spider destination STILL not FD");
                        }
                    }
                    else if (spider.agent.pathPending || spider.agent.isPathStale) // If the Spider is still calculating the path
                    {
                        Main.Log.LogInfo($"{DateTime.Now.ToString("HH:mm:ss")} RS2 Path Pending: {spider.agent.pathPending}\n Path Stale: {spider.agent.isPathStale}");
                        return; // leave
                    }
                    else
                    {
                        Main.Log.LogInfo($"{DateTime.Now.ToString("HH:mm:ss")} processed {processTimer} RUN ; {canRun}");
                        // Make the Spider run
                        spider.spiderSpeed = 14f;
                        spider.agent.speed = 14f;

                        if (spider.agent.destination != flightDestination || spider.agent.pathEndPosition != flightDestination && !setDest && canRun) // If its the first time
                        {
                            NavMeshPath path = new NavMeshPath();
                            NavMesh.CalculatePath(spider.transform.position, flightDestination, NavMesh.AllAreas, path);

                            // Set the path for the spider
                            spider.agent.SetPath(path);
                            Main.Log.LogInfo($"Set Path 1 {path}");
                            setDest = true;
                        }

                    }
                    Main.Log.LogInfo($"{DateTime.Now.ToString("HH:mm:ss")} {spider} Decided to RUN");
                    break;
                case 3: // Stun
                    spider.spiderSpeed = 0f;
                    spider.agent.speed = 0f;
                    spider.creatureAnimator.SetBool("moving", true);
                    break;
                default: return;
            }

        }
        private static void DeterminePath(SandSpiderAI spider)
        {
            // Get the direction from the spider to the player
            Vector3 directionToPlayer = spider.transform.position - closestPlayer.transform.position;
            Main.Log.LogInfo($"{DateTime.Now.ToString("HH:mm:ss")} player direction: {directionToPlayer}");

            // Normalize the direction and scale it to a desired distance
            Vector3 flightDirection = directionToPlayer.normalized * 30;
            Main.Log.LogInfo($"{DateTime.Now.ToString("HH:mm:ss")} flight direction: {flightDirection}");

            // Calculate the destination point
            flightDestination = spider.transform.position + flightDirection;
            Main.Log.LogInfo($"{DateTime.Now.ToString("HH:mm:ss")} flight destination: {flightDestination}");

            // Ensure the destination is on the NavMesh
            NavMeshHit hit;
            if (NavMesh.SamplePosition(flightDestination, out hit, 30.0f, NavMesh.AllAreas))
            {
                Main.Log.LogInfo($"{DateTime.Now.ToString("HH:mm:ss")} {spider} can run there");

                // Set the spider's destination
                if (!IsPlayerInPath(spider, flightDestination) || Vector3.Distance(spider.transform.position, hit.position) >= 10f)
                {
                    canRun = true;
                    flightDestination = hit.position;
                    Main.Log.LogInfo($"{DateTime.Now.ToString("HH:mm:ss")} Find Destination 1");
                }
                else
                {
                    Main.Log.LogInfo($"{DateTime.Now.ToString("HH:mm:ss")} {spider} is trapped 1");
                    behaviorState = 1;
                }
            }
            else // If the destination is unreachable / doesnt exist
            {
                Main.Log.LogInfo($"{DateTime.Now.ToString("HH:mm:ss")} {spider} can't run in original direction");
                // Try alternative directions or switch to attack
                if (!TryFindAlternativePath(spider, directionToPlayer)) // If can't find an alternate path
                {
                    Main.Log.LogInfo($"{DateTime.Now.ToString("HH:mm:ss")} {spider} is trapped 2");
                    behaviorState = 1; // Switch to attack
                }
            }
        }

        private static void ResetSprayEffect()
        {
            SprayPaintItemPatch.IsEnemySprayed = false;
            sprayTimer = 0f;
            processTimer = 0f;
            SprayPaintItemPatch.Enem.agent.ResetPath();
            enemiesAffected = false;
            isEligible = false;
            targetSpider = null;
            setDest = false;
            Main.Log.LogInfo($"{DateTime.Now.ToString("HH:mm:ss")} {SprayPaintItemPatch.Enem} recovered.");
        }

        // Function to get a direction vector from an angle and a base direction
        static Vector3 DirectionFromAngle(float angleInDegrees, Vector3 direction)
        {
            // Rotate the vector by the angle around the Y-axis
            return Quaternion.Euler(0, angleInDegrees, 0) * direction;
        }

        // New method to try finding an alternative path
        static private bool TryFindAlternativePath(SandSpiderAI spider, Vector3 directionToPlayer)
        {
            float[] anglesToTry = { 45, -45, 90, -90, 135, -135, 180 };
            foreach (float angle in anglesToTry)
            {
                Vector3 newFlightDirection = DirectionFromAngle(angle, directionToPlayer.normalized) * 15;
                Vector3 newFlightDestination = spider.transform.position + newFlightDirection;
                NavMeshHit hit;

                if (NavMesh.SamplePosition(newFlightDestination, out hit, 30.0f, NavMesh.AllAreas))
                {
                    if (!IsPlayerInPath(spider, newFlightDestination) || Vector3.Distance(spider.transform.position, hit.position) >= 10f)
                    {
                        flightDestination = hit.position;
                        canRun = true;
                        Main.Log.LogInfo($"{DateTime.Now.ToString("HH:mm:ss")} Find Destination 2");
                    }
                    else
                    {
                        return false;
                    }
                    return true;
                }
            }
            return false;
        }

        private static bool IsPlayerInPath(SandSpiderAI spider, Vector3 flightDestination)
        {
            Vector3 toDestination = (flightDestination - spider.transform.position).normalized;
            Vector3 toPlayer = (closestPlayer.transform.position - spider.transform.position).normalized;

            // Calculate the angle between the two directions
            float angle = Vector3.Angle(toDestination, toPlayer);
            Main.Log.LogInfo($"{DateTime.Now.ToString("HH:mm:ss")} The angle between the player and destination is {angle}");

            // Threshold angle to decide if the player is "in the way"
            return (angle <= 45f || angle >= 315f);
        }

        // Below handles stunning the inputted enemy
        private static bool ProcessEnemyType<T>(T[] enemies) where T : EnemyAI
        {
            foreach (var enemy in enemies)
            {
                if (enemy != null &&
                    SprayPaintItemPatch.Enem.transform.position == enemy.transform.position)
                {
                    closestPlayer = enemy.GetClosestPlayer();
                    switch (behaviorState)
                    {
                        case 1: // Fight
                            enemy.agent.acceleration = 12f;
                            break;
                        case 2: // Flight

                            // Get the direction from the spider to the player
                            Vector3 directionToPlayer = enemy.transform.position - closestPlayer.transform.position;
                            Main.Log.LogInfo($"{DateTime.Now.ToString("HH:mm:ss")} player direction: {directionToPlayer}");
                            // Normalize the direction and scale it to a desired distance
                            Vector3 flightDirection = directionToPlayer.normalized * 50;
                            Main.Log.LogInfo($"{DateTime.Now.ToString("HH:mm:ss")} flight direction: {flightDirection}");
                            // Calculate the destination point
                            flightDestination = enemy.transform.position + flightDirection;
                            Main.Log.LogInfo($"{DateTime.Now.ToString("HH:mm:ss")} flight destination: {flightDestination}");
                            /*// Ensure the destination is on the NavMesh
                            NavMeshHit hit;
                            if (NavMesh.SamplePosition(flightDestination, out hit, 10.0f, NavMesh.AllAreas))
                            {
                                flightDestination = hit.position;
                            }
                            else
                            {
                                // Handle case where no valid destination was found
                                // You might want to choose a different direction or fallback behavior
                                Main.Log.LogInfo($"{spider} cant run");
                            }*/

                            // Set the spider's destination
                            enemy.agent.SetDestination(flightDestination);
                            //spider.agent.speed = 9f;
                            //spider.spiderSpeed = 9f;
                            Main.Log.LogInfo($"{DateTime.Now.ToString("HH:mm:ss")} {enemy} Decided to RUN");
                            break;
                        case 3: // Deer In Headlights
                            enemy.agent.speed = 0f;
                            break;
                        default:
                            return false;
                    }
                    return true;
                }
            }
            return false;
        }
    }
    [HarmonyPatch(typeof(Terminal))]
    internal class SprayPricePatch
    {
        [HarmonyPatch("SetItemSales")]
        [HarmonyPostfix]
        private static void StorePrices(ref Item[] ___buyableItemsList)
        {
            ___buyableItemsList[12].creditsWorth = 400;
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