using HarmonyLib;
using TMPro;

namespace BetterContinents;

public partial class BetterContinents
{
    [HarmonyPatch(typeof(FejdStartup))]
    private class FejdStartupPatch
    {
        // 1.0.15: FejdStartup.m_connectionFailedError is TMPro.TMP_Text (was UnityEngine.UI.Text).
        // Harmony's ___field injection requires the parameter type to match the actual field type,
        // so keeping this as UnityEngine.UI.Text would make the patch fail at apply/JIT time.
        [HarmonyPostfix, HarmonyPatch("ShowConnectError")]
        private static void ShowConnectErrorPrefix(TMP_Text ___m_connectionFailedError)
        {
            if (LastConnectionError != null)
            {
                ___m_connectionFailedError.text = LastConnectionError;
                LastConnectionError = null;
            }
        }
        [HarmonyPrefix, HarmonyPatch("Awake")]
        static void AwakePrefix(FejdStartup __instance)
        {
            // Unpatching everything on main menu means other patches don't have to check for main menu.
            Settings.EnabledForThisWorld = false;
            DynamicPatch();
        }
        private static readonly Presets presets = new();

        [HarmonyPostfix, HarmonyPatch("Start")]
        static void StartPostfix(FejdStartup __instance)
        {
            Log("Start postfix");
            presets.InitUI(__instance);
        }
        [HarmonyPrefix, HarmonyPatch("OnNewWorldDone")]
        static void OnNewWorldDonePrefix()
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
