using GameNetcodeStuff;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.Windows;
using UnityEngine;
using System.Net;
using UnityEngine.InputSystem;

namespace LethalPlus.Patches
{
    [HarmonyPatch(typeof(PlayerControllerB))]
    internal class PlayerControllerBPatch
    {
        private static bool pKeyPressed = false;

        [HarmonyPostfix]
        [HarmonyPatch("Update")]
        private static void UpdatePostfix(PlayerControllerB __instance)
        {
            // Check for "P" key press using the new Input System
            if (Keyboard.current.pKey.wasPressedThisFrame)
            {
                // Only set the flag if it's not already set
                if (!pKeyPressed)
                {
                    pKeyPressed = true;

                    // Your logic here
                    Main.Log.LogInfo("P key pressed!");

                    // Check if there are spiders in the array
                    if (SprayPaintItemPatch.Spiders != null && SprayPaintItemPatch.Spiders.Length > 0)
                    {
                        int index = SprayPaintItemPatch.Spiders.Length - 1;

                        // Check if the spider is not null
                        if (index >= 0 && SprayPaintItemPatch.Spiders[index] != null)
                        {
                            SandSpiderAI spider = SprayPaintItemPatch.Spiders[index];

                            // Check if the agent is not null before accessing its properties
                            if (spider.agent != null)
                            {
                                if (__instance != null)
                                {
                                    spider.agent.Warp(__instance.transform.position);
                                }
                                else
                                {
                                    Main.Log.LogError("Player is null.");
                                }
                            }
                            else
                            {
                                Main.Log.LogError("Spider agent is null.");
                            }
                        }
                        else
                        {
                            Main.Log.LogError("Spider at index " + index + " is null.");
                        }
                    }
                    else
                    {
                        Main.Log.LogError("No spiders in the array.");
                    }
                }
            }
            else if (Keyboard.current.pKey.wasReleasedThisFrame)
            {
                // Reset the flag when the "P" key is released
                pKeyPressed = false;
            }
        }
    }

}
