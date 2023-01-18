using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

//###########################################################################
// Fix issue with `ApproachAndAttackTarget` and `SetNearestEntityAsTarget`.
// These properties are comma separated lists of classes, which is bad for
// us, since our own types must be referenced by fully qualifying the name.
// E.g. `EntityAirDrop, QuestCore`. Since we can't use the comma here, I
// simply patched the functions to replace `:` with `,` after the initial
// splitting on commas is done (so use colons instead of comma there).
//###########################################################################

public class FixEntityTargetFromMods
{

    [HarmonyPatch] class FixEntityTargetFromModsPatch
    {

        //#####################################################
        // The helper function to convert the string
        // Consuming a string from stack and putting one back
        //#####################################################

        static string DecodeClassNameFromCommaList(string fqcn)
        {
            return fqcn.Replace(':', ',');
        }

        //#####################################################
        //#####################################################

        static IEnumerable<MethodBase> TargetMethods()
        {
            yield return AccessTools.Method(typeof(EAISetNearestEntityAsTarget), "SetData");
            yield return AccessTools.Method(typeof(EAIApproachAndAttackTarget), "SetData");
        }

        //#####################################################
        //#####################################################

        static IEnumerable<CodeInstruction> Transpiler(
            IEnumerable<CodeInstruction> instructions)
        {
            var codes = new List<CodeInstruction>(instructions);
            // Patch all instances where we call directly from `Quest`
            for (var i = 0; i < codes.Count; i++)
            {
                if (codes[i].opcode != OpCodes.Call) continue;
                if (!(codes[i].operand is MethodInfo call)) continue;
                if (call.Name != "GetEntityType") continue;
                if (call.ReturnType != typeof(Type)) continue;
                // Very simple patch, as we just call a function
                // that will consume the argument on the stack
                // and put the decoded string back onto it.
                codes.Insert(i++, CodeInstruction.Call(
                                typeof(FixEntityTargetFromModsPatch),
                                "DecodeClassNameFromCommaList"));
            }
            return codes;
        }

        //#####################################################
        //#####################################################

    }
}