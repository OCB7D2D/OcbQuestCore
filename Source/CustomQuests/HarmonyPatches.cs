using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Xml.Linq;

//###########################################################################
//###########################################################################

public class CustomQuestsPatches
{

    //########################################################
    // In order to support custom quests and quest creators,
    // we need a way to set this up via new xml options.
    //########################################################

    // Static array to hold all quest creator classes
    static readonly Dictionary<string, IOcbQuestCreator>
        QuestCreators = new Dictionary<string, IOcbQuestCreator>();

    // Create "Creators" when QuestClass is parsed
    [HarmonyPatch(typeof(QuestsFromXml), "ParseQuest")]
    private class QuestsFromXmlParseQuest
    {
        private static void Postfix(XElement e)
        {
            string id = e.HasAttribute("id") ? e.GetAttribute("id") :
                throw new Exception("quest must have an id attribute");
            if (!e.HasAttribute("creator")) return;
            string QuestCreator = e.GetAttribute("creator");
            try
            {
                Type type = ReflectionHelpers.GetTypeWithPrefix("QuestCreator", QuestCreator);
                if (!(Activator.CreateInstance(type) is IOcbQuestCreator creator)) return;
                creator.ParseXmlConfig(e);
                QuestCreators[id] = creator;
            }
            catch (Exception ex)
            {
                throw new Exception("No quest creator class 'QuestCreator" + QuestCreator + "' found!", ex);
            }
        }
    }

    // Use "creators" when actual Quest is created
    [HarmonyPatch(typeof(QuestClass), "CreateQuest", new Type[] { })]
    public class QuestClassCreateQuest
    {
        private static bool Prefix(QuestClass __instance, ref Quest __result)
        {
            if (QuestCreators.TryGetValue(__instance.ID, out var creator))
            {
                __result = creator.CreateQuest(__instance);
                return false;
            }
            return true;
        }
    }

    //########################################################
    // Patch `HighestPhase` to support dynamic amount of objectives
    //########################################################

    // Call this instead of reading static `QuestClass.HightestPhase`
    // Pretty simple to get this from the registered objectives
    // For dynamic quests number of objectives can vary
    static byte GetDynamicHighestPhase(Quest quest)
    {
        if (quest is QuestCustom custom)
        {
            if (quest.Objectives == null)
                return quest.QuestClass.HighestPhase;
            if (quest.Objectives.Count == 0)
                return quest.QuestClass.HighestPhase;
            var idx = quest.Objectives.Count - 1;
            return quest.Objectives[idx].Phase;
        }
        return quest.QuestClass.HighestPhase;
    }

    // Can't patch this directly, as objectes are read afterwards
    // Therefore we use a Postfix to update the value afterwards
    [HarmonyPatch(typeof(Quest), "Read")]
    private class QuestRead
    {
        static void Postfix(Quest __instance)
        {
            if (__instance.CurrentState == Quest.QuestState.Completed)
                __instance.CurrentPhase = GetDynamicHighestPhase(__instance);
            else if (__instance.CurrentState == Quest.QuestState.ReadyForTurnIn)
                __instance.CurrentPhase = GetDynamicHighestPhase(__instance);
        }
    }

    [HarmonyPatch]
    public class PatchDynamicHighestPhase
    {

        static IEnumerable<MethodBase> TargetMethods()
        {
            yield return AccessTools.Method(typeof(XUiC_QuestEntry), "GetBindingValue");
            yield return AccessTools.Method(typeof(QuestJournal), "FailAllSharedQuests");
            yield return AccessTools.Method(typeof(QuestJournal), "FailAllActivatedQuests");
            yield return AccessTools.Method(typeof(QuestJournal), "RemoveAllSharedQuests");
            yield return AccessTools.Method(typeof(QuestJournal), "StartQuests");
            yield return AccessTools.Method(typeof(Quest), "ResetToRallyPointObjective");
            yield return AccessTools.Method(typeof(Quest), "CheckForCompletion");
        }

        static IEnumerable<CodeInstruction> Transpiler(
            IEnumerable<CodeInstruction> instructions)
        {
            var codes = new List<CodeInstruction>(instructions);
            // Patch all instances where we call directly from `Quest`
            for (var i = 0; i < codes.Count; i++)
            {
                if (codes[i].opcode != OpCodes.Ldarg_0) continue;
                if (++i >= codes.Count) break;
                if (codes[i].opcode != OpCodes.Ldfld) continue;
                if (!(codes[i].operand is FieldInfo field)) continue;
                if (field.FieldType != typeof(QuestClass)) continue;
                if (++i >= codes.Count) break;
                if (codes[i].opcode != OpCodes.Callvirt) continue;
                if (!(codes[i].operand is MethodInfo call)) continue;
                if (call.Name != "get_HighestPhase") continue;
                // Instead of the `questClass` field, but the
                // actual quest from XUiC class on the stack
                codes[i - 1] = codes[i - 4];
                // Call our funcion instead `Quest.get_HigestPhase`
                codes[i] = CodeInstruction.Call(
                    typeof(CustomQuestsPatches),
                    "GetDynamicHighestPhase");
            }
            // Patch all instances ...
            for (var i = 0; i < codes.Count; i++)
            {
                if (codes[i].opcode != OpCodes.Callvirt) continue;
                if (++i >= codes.Count) break;
                if (codes[i].opcode != OpCodes.Callvirt) continue;
                if (!(codes[i].operand is MethodInfo klass)) continue;
                if (klass.Name != "get_QuestClass") continue;
                if (klass.ReturnType != typeof(QuestClass)) continue;
                if (++i >= codes.Count) break;
                if (codes[i].opcode != OpCodes.Callvirt) continue;
                if (!(codes[i].operand is MethodInfo call)) continue;
                if (call.Name != "get_HighestPhase") continue;
                // Call our funcion instead `Quest.get_HigestPhase`
                codes[i] = CodeInstruction.Call(
                    typeof(CustomQuestsPatches),
                    "GetDynamicHighestPhase");
                // Remove the opcode to resolve `QuestClass`
                // We need to work directly with the `Quest`
                codes.RemoveAt(i - 1);
            }
            // Patch all instances ...
            for (var i = 0; i < codes.Count; i++)
            {
                if (codes[i].opcode != OpCodes.Ldarg_0) continue;
                if (++i >= codes.Count) break;
                if (codes[i].opcode != OpCodes.Call) continue;
                if (!(codes[i].operand is MethodInfo quest)) continue;
                if (quest.Name != "get_CurrentPhase") continue;
                if (++i >= codes.Count) break;
                if (codes[i].opcode != OpCodes.Ldloc_0) continue;
                if (++i >= codes.Count) break;
                if (codes[i].opcode != OpCodes.Callvirt) continue;
                if (!(codes[i].operand is MethodInfo call)) continue;
                if (call.Name != "get_HighestPhase") continue;
                // Put the main quest object on the stack again
                // Replaces the opcode that puts the quest on stack
                codes[i - 1] = new CodeInstruction(OpCodes.Ldarg_0);
                // Call our funcion instead `Quest.get_HigestPhase`
                codes[i] = CodeInstruction.Call(
                    typeof(CustomQuestsPatches),
                    "GetDynamicHighestPhase");
            }
            // Used to debug IL code around patch
            // for (int n = i - 3; n < i + 3; n++)
            // {
            //     Log.Out("   ==> {0}", codes[n]);
            // }
            return codes;
        }

    }

    //########################################################
    //########################################################

}

