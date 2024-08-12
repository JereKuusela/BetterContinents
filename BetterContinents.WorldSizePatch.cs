using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;
namespace BetterContinents;

public class WorldSizeHelper
{

  private static bool EdgeCheckPatched = false;
  private static bool WorldSizePatched = false;
  private static float WorldRadius = 0f;
  private static float EdgeSize = 0f;
  private static float WorldTotalRadius = 0f;
  private static float WorldStretch = 1f;
  private static float BiomeStretch = 1f;
  public static void PatchEdgeChecks(Harmony harmony, float worldSize, float edgeSize)
  {
    var toPatch = worldSize != 15000f || edgeSize != 500f;
    if (toPatch == EdgeCheckPatched) return;
    WorldRadius = worldSize;
    EdgeSize = edgeSize;
    WorldTotalRadius = WorldRadius + EdgeSize;
    EdgeCheckPatched = toPatch;
    PatchApplyEdgeForce(harmony);
    PatchEdgeOfWorldKill(harmony);
    PatchSetupMaterial(harmony);
    PatchScaleGlobalWaterSurface(harmony);
    PatchUpdateWind(harmony);
    PatchWaterSurface(harmony);
  }

  private static void PatchApplyEdgeForce(Harmony harmony)
  {
    var method = AccessTools.Method(typeof(Ship), nameof(Ship.ApplyEdgeForce));
    var patch = AccessTools.Method(typeof(WorldSizeHelper), nameof(ApplyEdgeForceTranspiler));
    if (EdgeCheckPatched)
      harmony.Patch(method, transpiler: new HarmonyMethod(patch));
    else
      harmony.Unpatch(method, patch);
  }
  private static IEnumerable<CodeInstruction> ApplyEdgeForceTranspiler(IEnumerable<CodeInstruction> instructions) => ModifyEdgeCheck(instructions);

  private static void PatchEdgeOfWorldKill(Harmony harmony)
  {
    var method = AccessTools.Method(typeof(Player), nameof(Player.EdgeOfWorldKill));
    var prefix = AccessTools.Method(typeof(WorldSizeHelper), nameof(EdgeOfWorldKillPrefix));
    var transpiler = AccessTools.Method(typeof(WorldSizeHelper), nameof(EdgeOfWorldKillTranspiler));
    if (EdgeCheckPatched)
    {
      harmony.Patch(method, prefix: new HarmonyMethod(prefix), transpiler: new HarmonyMethod(transpiler));
    }
    else
    {
      harmony.Unpatch(method, prefix);
      harmony.Unpatch(method, transpiler);
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
    if (WorldSizePatched)
    {
      harmony.Patch(method, prefix: new HarmonyMethod(prefix));
    }
    else
    {
      harmony.Unpatch(method, prefix);
    }
    RefreshSetupMaterial();
  }
  private static void RefreshSetupMaterial()
  {
    var objects = Object.FindObjectsOfType<WaterVolume>();
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
    if (WorldSizePatched)
    {
      harmony.Patch(method, postfix: new HarmonyMethod(postfix));
    }
    else
    {
      harmony.Unpatch(method, postfix);
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
    if (WorldSizePatched)
      harmony.Patch(method, transpiler: new HarmonyMethod(transpiler));
    else
      harmony.Unpatch(method, transpiler);
  }
  private static IEnumerable<CodeInstruction> UpdateWindTranspiler(IEnumerable<CodeInstruction> instructions)
  {
    CodeMatcher matcher = new(instructions);
    matcher = Replace(matcher, 10500f, WorldTotalRadius);
    // Removes the subtraction of m_edgeOfWorldWidth (already applied above).
    matcher = matcher
      .SetOpcodeAndAdvance(OpCodes.Nop)
      .SetOpcodeAndAdvance(OpCodes.Nop)
      .SetOpcodeAndAdvance(OpCodes.Nop);
    matcher = Replace(matcher, 10500f, WorldTotalRadius);
    // Removes the subtraction of m_edgeOfWorldWidth (already applied above).
    matcher = matcher
      .SetOpcodeAndAdvance(OpCodes.Nop)
      .SetOpcodeAndAdvance(OpCodes.Nop)
      .SetOpcodeAndAdvance(OpCodes.Nop);
    matcher = Replace(matcher, 10500f, WorldTotalRadius);

    return matcher.InstructionEnumeration();
  }

  private static void PatchWaterSurface(Harmony harmony)
  {
    var method = AccessTools.Method(typeof(WaterVolume), nameof(WaterVolume.GetWaterSurface));
    var transpiler = AccessTools.Method(typeof(WorldSizeHelper), nameof(GetWaterSurfaceTranspiler));
    if (WorldSizePatched)
      harmony.Patch(method, transpiler: new HarmonyMethod(transpiler));
    else
      harmony.Unpatch(method, transpiler);
  }

  private static IEnumerable<CodeInstruction> GetWaterSurfaceTranspiler(IEnumerable<CodeInstruction> instructions)
  {
    CodeMatcher matcher = new(instructions);
    matcher = Replace(matcher, 10500f, WorldTotalRadius);
    return matcher.InstructionEnumeration();
  }

  public static void PatchWorldSize(Harmony harmony, float worldSize, float edgeSize)
  {
    var toPatch = worldSize != 15000f || edgeSize != 500f;
    if (toPatch == WorldSizePatched) return;
    WorldRadius = worldSize;
    EdgeSize = edgeSize;
    WorldTotalRadius = WorldRadius + EdgeSize;
    WorldSizePatched = toPatch;
    PatchGetAshlandsHeight(harmony);
    PatchGetBaseHeight(harmony);
    if (toPatch) EWD.RefreshSize(WorldRadius, WorldTotalRadius, WorldStretch, BiomeStretch);
  }
  private static void PatchGetAshlandsHeight(Harmony harmony)
  {
    var method = AccessTools.Method(typeof(WorldGenerator), nameof(WorldGenerator.GetAshlandsHeight));
    var patch = AccessTools.Method(typeof(WorldSizeHelper), nameof(GetAshlandsHeightTranspiler));
    if (WorldSizePatched)
      harmony.Patch(method, transpiler: new HarmonyMethod(patch));
    else
      harmony.Unpatch(method, patch);
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
    if (WorldSizePatched)
      harmony.Patch(method, transpiler: new HarmonyMethod(patch));
    else
      harmony.Unpatch(method, patch);
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

  private static CodeMatcher Replace(CodeMatcher instructions, double value, double newValue)
  {
    return instructions
      .MatchForward(false, new CodeMatch(OpCodes.Ldc_R8, value))
      .SetOperandAndAdvance(newValue);
  }
  private static CodeMatcher Replace(CodeMatcher instructions, float value, float newValue)
  {
    return instructions
      .MatchForward(false, new CodeMatch(OpCodes.Ldc_R4, value))
      .SetOperandAndAdvance(newValue);
  }
}
