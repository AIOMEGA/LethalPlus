using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LethalPlus.Patches
{
    [HarmonyPatch(typeof(CrawlerAI))]
    internal class CrawlerPatch
    {
        [HarmonyPatch(nameof(CrawlerAI.HitEnemy))] // nameof only works when the method is public ; if private replace it with "'MethodName'"
        [HarmonyPrefix]
        static void RailingDodgePatch() // With v45 you could no longer jump on railings to defend against the Cralwer, personally I think it made sense
        {

        }
    }
}
