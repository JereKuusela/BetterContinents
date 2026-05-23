// 0.0.3c
using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;
namespace BetterContinents;

public class WorldSizeHelper
{
    /**
     * Earlier (0.0.2c) the gate was `if (toPatch == EdgeCheckPatched) return;`
     * where toPatch was a boolean — "is patching needed at all". That gate
     * skips re-patching whenever the boolean state is unchanged, even if the
     * actual radius VALUES have changed. User-visible symptom was the toggle
     * bug: going from DisableMapEdgeDropoff=true (1E30) to false (real
     * WorldSize, e.g. 25000) both produce toPatch=true, gate fires, IL stays
     * baked with 1E30, dropoff appears stuck off. Reverse direction has the
     * same problem.
     *
     * Fix is to memoise the last (worldSize, edgeSize) pair we actually
     * patched and gate on value-equality. NaN sentinels guarantee the first
     * call fires.
     *
     * Second part of the fix is in each sub-patch helper below: harmony.Patch
     * with the same patch method is a no-op (Harmony deduplicates), so the
     * static-field-driven transpiler delegate never gets a chance to re-bake
     * IL with the new constants. We Unpatch first, then Patch — that forces
     * the wrapper rebuild and the transpiler runs again with current values.
     */
    private static bool EdgeCheckPatched = false;
    private static bool WorldSizePatched = false;
    private static float WorldRadius = 0f;
    private static float EdgeSize = 0f;
    private static float WorldTotalRadius = 0f;
    private static float WorldStretch = 1f;
    private static float BiomeStretch = 1f;
    private static float LastEdgeWorldSize = float.NaN;
    private static float LastEdgeSize = float.NaN;
    private static float LastWorldSizeWorldSize = float.NaN;
    private static float LastWorldSizeEdgeSize = float.NaN;

    public static void PatchEdgeChecks(Harmony harmony, float worldSize, float edgeSize)
    {
        if (worldSize == LastEdgeWorldSize && edgeSize == LastEdgeSize) return;
        LastEdgeWorldSize = worldSize;
        LastEdgeSize = edgeSize;

        WorldRadius = worldSize;
        EdgeSize = edgeSize;
        WorldTotalRadius = WorldRadius + EdgeSize;
        EdgeCheckPatched = worldSize != 10000f || edgeSize != 500f;

        PatchApplyEdgeForce(harmony);
        PatchEdgeOfWorldKill(harmony);
        PatchSetupMaterial(harmony);
        PatchScaleGlobalWaterSurface(harmony);
        PatchUpdateWind(harmony);
        PatchWaterSurface(harmony);
        PatchBiomeHeight(harmony);
        PatchGetBaseHeight(harmony);
    }
    public static float GetWorldStretch() => WorldStretch;

    public static void SetStretch(float worldStretch, float biomeStretch)
    {
        WorldStretch = worldStretch;
        BiomeStretch = biomeStretch;
        BetterContinents.Log($"[WorldSizeHelper] Updated stretches: World={WorldStretch}, Biome={BiomeStretch}");
        // Refresh EWD with new values
        if (WorldSizePatched) EWD.RefreshSize(WorldRadius, WorldTotalRadius, WorldStretch, BiomeStretch);
    }

    private static void PatchApplyEdgeForce(Harmony harmony)
    {
        var method = AccessTools.Method(typeof(Ship), nameof(Ship.ApplyEdgeForce));
        var patch = AccessTools.Method(typeof(WorldSizeHelper), nameof(ApplyEdgeForceTranspiler));
        harmony.Unpatch(method, patch);
        if (EdgeCheckPatched)
            harmony.Patch(method, transpiler: new HarmonyMethod(patch));
    }
    private static IEnumerable<CodeInstruction> ApplyEdgeForceTranspiler(IEnumerable<CodeInstruction> instructions) => ModifyEdgeCheck(instructions);

    private static void PatchEdgeOfWorldKill(Harmony harmony)
    {
        var method = AccessTools.Method(typeof(Player), nameof(Player.EdgeOfWorldKill));
        var prefix = AccessTools.Method(typeof(WorldSizeHelper), nameof(EdgeOfWorldKillPrefix));
        var transpiler = AccessTools.Method(typeof(WorldSizeHelper), nameof(EdgeOfWorldKillTranspiler));
        harmony.Unpatch(method, prefix);
        harmony.Unpatch(method, transpiler);
        if (EdgeCheckPatched)
        {
            harmony.Patch(method, prefix: new HarmonyMethod(prefix), transpiler: new HarmonyMethod(transpiler));
        }
    }
    private static IEnumerable<CodeInstruction> EdgeOfWorldKillTranspiler(IEnumerable<CodeInstruction> instructions) => ModifyEdgeCheck(instructions);
    // Safer to simply skip when in dungeons.
    private static bool EdgeOfWorldKillPrefix(Player __instance) => __instance.transform.position.y < 4000f;


    private static IEnumerable<CodeInstruction> ModifyEdgeCheck(IEnumerable<CodeInstruction> instructions)
    {
        CodeMatcher matcher = new(instructions);
        matcher = Replace(matcher, 10420f, WorldTotalRadius - 80);
        matcher = Replace(matcher, 10500f, WorldTotalRadius);
        return matcher.InstructionEnumeration();
    }

    private static void PatchSetupMaterial(Harmony harmony)
    {
        var method = AccessTools.Method(typeof(WaterVolume), nameof(WaterVolume.SetupMaterial));
        var prefix = AccessTools.Method(typeof(WorldSizeHelper), nameof(SetupMaterialPrefix));
        harmony.Unpatch(method, prefix);
        if (EdgeCheckPatched)
        {
            harmony.Patch(method, prefix: new HarmonyMethod(prefix));
        }
        RefreshSetupMaterial();
    }
    private static void RefreshSetupMaterial()
    {
        var objects = Object.FindObjectsByType<WaterVolume>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (var water in objects)
        {
            water.m_waterSurface.material.SetFloat("_WaterEdge", WorldTotalRadius);
        }
    }
    private static void SetupMaterialPrefix(WaterVolume __instance)
    {
        __instance.m_waterSurface.material.SetFloat("_WaterEdge", WorldTotalRadius);
    }
    private static void PatchScaleGlobalWaterSurface(Harmony harmony)
    {
        var method = AccessTools.Method(typeof(EnvMan), nameof(EnvMan.Awake));
        var postfix = AccessTools.Method(typeof(WorldSizeHelper), nameof(ScaleGlobalWaterSurfacePostFix));
        harmony.Unpatch(method, postfix);
        if (EdgeCheckPatched)
        {
            harmony.Patch(method, postfix: new HarmonyMethod(postfix));
        }
        if (EnvMan.instance)
            ScaleGlobalWaterSurface(EnvMan.instance);
    }
    private static void ScaleGlobalWaterSurface(EnvMan obj)
    {
        var water = obj.transform.Find("WaterPlane").Find("watersurface");
        water.GetComponent<MeshRenderer>().material.SetFloat("_WaterEdge", WorldTotalRadius);
    }
    private static void ScaleGlobalWaterSurfacePostFix(EnvMan __instance) => ScaleGlobalWaterSurface(__instance);


    private static void PatchUpdateWind(Harmony harmony)
    {
        var method = AccessTools.Method(typeof(EnvMan), nameof(EnvMan.UpdateWind));
        var transpiler = AccessTools.Method(typeof(WorldSizeHelper), nameof(UpdateWindTranspiler));
        harmony.Unpatch(method, transpiler);
        if (EdgeCheckPatched)
            harmony.Patch(method, transpiler: new HarmonyMethod(transpiler));
    }
    /**
     * The first two anchors stand in for the IL sequence
     * (ldc.r4 10500 / ldarg.0 / ldfld m_edgeOfWorldWidth / sub), i.e. the inner
     * falloff boundary expressed as outer-minus-edge. The 3xNOP block removes
     * the subtraction, so the substituted constant must already be the inner
     * radius. Using WorldTotalRadius here pushed the wind transition outside
     * the playable area by exactly EdgeSize, which is wrong for any non-default
     * edge size. The third anchor is a bare outer radius with no subtraction
     * following it, and stays WorldTotalRadius.
     */
    private static IEnumerable<CodeInstruction> UpdateWindTranspiler(IEnumerable<CodeInstruction> instructions)
    {
        CodeMatcher matcher = new(instructions);

        // Patch 1
        matcher = Replace(matcher, 10500f, WorldRadius);
        if (!matcher.IsInvalid) // Only Nop if we actually found and replaced the anchor
        {
            matcher.SetOpcodeAndAdvance(OpCodes.Nop)
                   .SetOpcodeAndAdvance(OpCodes.Nop)
                   .SetOpcodeAndAdvance(OpCodes.Nop);
        }

        // Patch 2
        matcher = Replace(matcher, 10500f, WorldRadius);
        if (!matcher.IsInvalid)
        {
            matcher.SetOpcodeAndAdvance(OpCodes.Nop)
                   .SetOpcodeAndAdvance(OpCodes.Nop)
                   .SetOpcodeAndAdvance(OpCodes.Nop);
        }

        // Patch 3
        matcher = Replace(matcher, 10500f, WorldTotalRadius);

        return matcher.InstructionEnumeration();
    }

    private static void PatchWaterSurface(Harmony harmony)
    {
        var method = AccessTools.Method(typeof(WaterVolume), nameof(WaterVolume.GetWaterSurface));
        var transpiler = AccessTools.Method(typeof(WorldSizeHelper), nameof(ReplaceTotalSize));
        harmony.Unpatch(method, transpiler);
        if (WorldSizePatched)
            harmony.Patch(method, transpiler: new HarmonyMethod(transpiler));
    }

    /**
     * GetWaterSurface receives world-space coordinates (no stretch applied),
     * so the literal 10500f at the kill-radius check is the outer radius in
     * world units and gets replaced with WorldTotalRadius directly.
     */
    private static IEnumerable<CodeInstruction> ReplaceTotalSize(IEnumerable<CodeInstruction> instructions)
      => Replace(new(instructions), 10500f, WorldTotalRadius).InstructionEnumeration();

    private static void PatchBiomeHeight(Harmony harmony)
    {
        var method = AccessTools.Method(typeof(WorldGenerator), nameof(WorldGenerator.GetBiomeHeight));
        var transpiler = AccessTools.Method(typeof(WorldSizeHelper), nameof(ReplaceTotalSizeStretched));
        harmony.Unpatch(method, transpiler);
        if (EdgeCheckPatched)
            harmony.Patch(method, transpiler: new HarmonyMethod(transpiler));
    }

    /**
     * GetBiomeHeight is called with PRE-STRETCH coordinates: EWS's stretch
     * prefix on the same method divides wx/wy by WorldStretch before the body
     * runs, so the literal 10500f compares against shrunken inputs. The
     * replacement constant therefore has to be shrunk too, i.e. divided by
     * WorldStretch.
     */
    private static IEnumerable<CodeInstruction> ReplaceTotalSizeStretched(IEnumerable<CodeInstruction> instructions)
      => Replace(new(instructions), 10500f, WorldTotalRadius / WorldStretch).InstructionEnumeration();

    public static void PatchWorldSize(Harmony harmony, float worldSize, float edgeSize)
    {
        if (worldSize == LastWorldSizeWorldSize && edgeSize == LastWorldSizeEdgeSize) return;
        LastWorldSizeWorldSize = worldSize;
        LastWorldSizeEdgeSize = edgeSize;

        WorldRadius = worldSize;
        EdgeSize = edgeSize;
        WorldTotalRadius = WorldRadius + EdgeSize;
        WorldSizePatched = worldSize != 10000f || edgeSize != 500f;

        PatchGetAshlandsHeight(harmony);
        if (WorldSizePatched) EWD.RefreshSize(WorldRadius, WorldTotalRadius, WorldStretch, BiomeStretch);
    }
    private static void PatchGetAshlandsHeight(Harmony harmony)
    {
        var method = AccessTools.Method(typeof(WorldGenerator), nameof(WorldGenerator.GetAshlandsHeight));
        var patch = AccessTools.Method(typeof(WorldSizeHelper), nameof(GetAshlandsHeightTranspiler));
        harmony.Unpatch(method, patch);
        if (WorldSizePatched)
            harmony.Patch(method, transpiler: new HarmonyMethod(patch));
    }
    private static IEnumerable<CodeInstruction> GetAshlandsHeightTranspiler(IEnumerable<CodeInstruction> instructions)
    {
        CodeMatcher matcher = new(instructions);
        matcher = Replace(matcher, 10150d, WorldTotalRadius / WorldStretch);
        return matcher.InstructionEnumeration();
    }

    private static void PatchGetBaseHeight(Harmony harmony)
    {
        var method = AccessTools.Method(typeof(WorldGenerator), nameof(WorldGenerator.GetBaseHeight));
        var patch = AccessTools.Method(typeof(WorldSizeHelper), nameof(GetBaseHeightTranspiler));
        harmony.Unpatch(method, patch);
        if (EdgeCheckPatched)
            harmony.Patch(method, transpiler: new HarmonyMethod(patch));
    }
    private static IEnumerable<CodeInstruction> GetBaseHeightTranspiler(IEnumerable<CodeInstruction> instructions)
    {
        var strechedWorldRadius = WorldRadius / WorldStretch;
        var strechedWorldTotalRadius = WorldTotalRadius / WorldStretch;
        CodeMatcher matcher = new(instructions);
        // Skipping the menu part.
        matcher = matcher.MatchForward(false, new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(WorldGenerator), nameof(WorldGenerator.m_offset1))));
        // Incoming coordinates are stretched, so all limits must be stretched as well.
        matcher = Replace(matcher, 10000f, strechedWorldRadius);
        matcher = Replace(matcher, 10000f, strechedWorldRadius);
        matcher = Replace(matcher, 10500f, strechedWorldTotalRadius);
        matcher = Replace(matcher, 10490f, (WorldTotalRadius - 10f) / WorldStretch);
        matcher = Replace(matcher, 10500f, strechedWorldTotalRadius);
        return matcher.InstructionEnumeration();
    }

    private static CodeMatcher Replace(CodeMatcher instructions, float value, float newValue)
    {
        instructions.MatchForward(false, new CodeMatch(OpCodes.Ldc_R4, value));

        if (instructions.IsInvalid)
        {
            BetterContinents.LogWarning($"[WorldSizeHelper] Replace: float {value} NOT FOUND");
            return instructions;
        }

        return instructions.SetOperandAndAdvance(newValue);
    }

    private static CodeMatcher Replace(CodeMatcher instructions, double value, double newValue)
    {
        instructions.MatchForward(false, new CodeMatch(OpCodes.Ldc_R8, value));

        if (instructions.IsInvalid)
        {
            BetterContinents.LogWarning($"[WorldSizeHelper] Replace: double {value} NOT FOUND");
            return instructions;
        }

        return instructions.SetOperandAndAdvance(newValue);
    }
}