using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;

namespace BetterContinents;

public partial class BetterContinents
{
  private static IEnumerable<CodeInstruction> GetBiomePatch(IEnumerable<CodeInstruction> instructions)
  {
    var precision = Settings.BiomePrecision;
    CodeMatcher matcher = new(instructions);
    matcher = matcher.End().MatchEndBackwards(new CodeMatch(OpCodes.Stind_R4));
    // Precision 1 = 3x3, 2 = 5x5, 3 = 7x7, etc.
    var last = precision + 1;
    var multiplier = 1f / last;
    for (int x = 0; x <= last; x++)
    {
      for (int y = 0; y <= last; y++)
      {
        // Corners are already handled.
        if ((x == 0 || x == last) && (y == 0 || y == last))
          continue;
        matcher = AddBiomeCheck(matcher, x * multiplier, y * multiplier);
      }
    }
    return matcher.InstructionEnumeration();
  }
  private static CodeMatcher AddBiomeCheck(CodeMatcher matcher, float x, float y) => matcher
      .InsertAndAdvance(new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(Heightmap), nameof(Heightmap.s_tempBiomeWeights))))
      .InsertAndAdvance(new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(Heightmap), nameof(Heightmap.s_biomeToIndex))))
      .InsertAndAdvance(new CodeInstruction(OpCodes.Ldarg_0))
      .InsertAndAdvance(new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(Heightmap), nameof(Heightmap.m_cornerBiomes))))
      .InsertAndAdvance(new CodeInstruction(OpCodes.Ldc_I4_3))
      .InsertAndAdvance(new CodeInstruction(OpCodes.Ldelem_I4))
      .InsertAndAdvance(new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(Dictionary<Heightmap.Biome, int>), "get_Item")))
      .InsertAndAdvance(new CodeInstruction(OpCodes.Ldelema, typeof(float)))
      .InsertAndAdvance(new CodeInstruction(OpCodes.Dup))
      .InsertAndAdvance(new CodeInstruction(OpCodes.Ldind_R4))
      .InsertAndAdvance(new CodeInstruction(OpCodes.Ldloc_0))
      .InsertAndAdvance(new CodeInstruction(OpCodes.Ldloc_1))
      .InsertAndAdvance(new CodeInstruction(OpCodes.Ldc_R4, x))
      .InsertAndAdvance(new CodeInstruction(OpCodes.Ldc_R4, y))
      .InsertAndAdvance(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Heightmap), nameof(Heightmap.Distance))))
      .InsertAndAdvance(new CodeInstruction(OpCodes.Add))
      .InsertAndAdvance(new CodeInstruction(OpCodes.Stind_R4));

}