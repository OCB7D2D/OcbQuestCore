using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

//###########################################################################
// The objective list in the journal has limited space (10 entries), so
// we want to hide certain objectives there. Unfortunately the built-in
// flag `HiddenObjective` will also hide the objective from the on-screen
// objective list, while a phase is active. But we only want to hide some
// in the journal list, but not on the on-screen list. This achieves that.
//###########################################################################

public class FixQuestJournalObjectiveList
{

    [HarmonyPatch] class FixQuestJournalObjectiveListPatch
    {

        //#####################################################
        //#####################################################

        static bool IsHiddenInJournalObjectiveList(BaseObjective objective)
        {
            if (objective.HiddenObjective) return true;
            if (objective is BaseCustomObjective custom)
            {
                return custom.IsVisible == false;
            }
            return false;
        }

        //#####################################################
        //#####################################################

        static IEnumerable<MethodBase> TargetMethods()
        {
            yield return AccessTools.Method(typeof(XUiC_QuestObjectiveList), "Update");
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
                if (codes[i].opcode != OpCodes.Ldfld) continue;
                if (!(codes[i].operand is FieldInfo field)) continue;
                if (field.Name != "HiddenObjective") continue;
                if (field.FieldType != typeof(bool)) continue;
                // Very simple patch, as we just replace a
                // field getter with our own function call,
                // taking an argument from the stack and
                // putting our result (bool) back onto it.
                codes[i] = CodeInstruction.Call(
                    typeof(FixQuestJournalObjectiveListPatch),
                    "IsHiddenInJournalObjectiveList");
            }
            return codes;
        }

        //#####################################################
        //#####################################################

    }
}