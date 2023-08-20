using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using HarmonyLib;

namespace BetterContinents;

public partial class BetterContinents
{
    [HarmonyPatch]
    private class SavePatch
    {
        [HarmonyPostfix, HarmonyPatch(typeof(SaveSystem), nameof(SaveSystem.GetSaveInfo))]
        static void GetSaveInfoPostfix(ref string saveName, ref string actualFileEnding)
        {
            // BC uses .fwl.BetterContinents extension. GetSaveInfo uses only the last extension.
            // So we need to remove the .fwl extension from the file name.
            if (saveName.EndsWith(".fwl", StringComparison.OrdinalIgnoreCase))
                saveName = saveName.Substring(0, saveName.Length - 4);
            // And add it back to the actualFileEnding.
            if (actualFileEnding == ConfigFileExtension)
                actualFileEnding = ".fwl" + ConfigFileExtension;
        }
        [HarmonyTranspiler, HarmonyPatch(typeof(SaveCollection), nameof(SaveCollection.Reload))]
        static IEnumerable<CodeInstruction> Reload(IEnumerable<CodeInstruction> instructions) => Transpile(instructions);

        [HarmonyTranspiler, HarmonyPatch(typeof(SaveFile), nameof(SaveFile.FileName), MethodType.Getter)]
        static IEnumerable<CodeInstruction> FileName(IEnumerable<CodeInstruction> instructions) => Transpile(instructions);

        // To recognize .BetterContinents extension.
        static IEnumerable<CodeInstruction> Transpile(IEnumerable<CodeInstruction> instructions)
        {
            var codes = new List<CodeInstruction>(instructions);
            for (var i = 0; i < codes.Count; i++)
            {
                var code = codes[i];
                if (code.opcode != OpCodes.Ldstr) continue;
                if (code.operand != (object)".fwl") continue;
                // Duplicate the previous instruction.
                codes.Insert(i - 1, codes[i - 1]);
                i += 1;
                // Add our extension.
                codes.Insert(i - 1, new CodeInstruction(OpCodes.Ldstr, ".fwl" + ConfigFileExtension));
                i += 1;
                // Duplicate the check.
                codes.Insert(i - 1, codes[i + 1]);
                i += 1;
                // Duplicate the jump.
                codes.Insert(i - 1, codes[i + 2]);
                i += 1;
            }
            return codes.AsEnumerable();
        }
    }
}
