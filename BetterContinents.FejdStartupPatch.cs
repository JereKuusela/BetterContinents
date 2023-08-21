using HarmonyLib;
using UnityEngine.UI;

namespace BetterContinents;

public partial class BetterContinents
{
    [HarmonyPatch(typeof(FejdStartup))]
    private class FejdStartupPatch
    {
        [HarmonyPostfix, HarmonyPatch("ShowConnectError")]
        private static void ShowConnectErrorPrefix(Text ___m_connectionFailedError)
        {
            if (LastConnectionError != null)
            {
                ___m_connectionFailedError.text = LastConnectionError;
                LastConnectionError = null;
            }
        }

        private static readonly Presets presets = new();

        [HarmonyPostfix, HarmonyPatch("Start")]
        private static void StartPostfix(FejdStartup __instance)
        {
            Log("Start postfix");
            presets.InitUI(__instance);
        }
        [HarmonyPrefix, HarmonyPatch("OnNewWorldDone")]
        private static void OnNewWorldDonePrefix()
        {
            // Indicator to SaveWorldMetaDataPostfix that it should save a new BC config file using the 
            // selected preset, rather than saving the active worlds settings.
            WorldPatch.bWorldBeingCreated = true;
            Log($"[Saving] Setting the bWorldBeingCreated flag");
        }

        [HarmonyPostfix, HarmonyPatch("OnNewWorldDone")]
        private static void OnNewWorldDonePostfix()
        {
            // Clear the flag again, ready for normal save operations 
            WorldPatch.bWorldBeingCreated = false;
            Log($"[Saving] Clearing the bWorldBeingCreated flag again");
        }
    }
}
