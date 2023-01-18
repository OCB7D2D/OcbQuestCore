
#if DEBUG

public class QuestDebug
{

    [HarmonyPatch(typeof(Quest), "RefreshPhase")]
    private class QuestRefreshPhasePatch
    {
        static void Prefix(Quest __instance)
        {
            Log.Out("============================================");
            Log.Out("Moved quest phase to {0} ({1})",
                __instance.CurrentPhase, __instance.Position);
            foreach (var pos in __instance.PositionData)
                Log.Out("  pos {0} => {1}", pos.Key, pos.Value);
            foreach (var data in __instance.DataVariables)
                Log.Out("  data {0} => {1}", data.Key, data.Value);
            Log.Out("============================================");
        }
    }

    [HarmonyPatch(typeof(Quest), "StartQuest")]
    private class QuesStartQuestPatch
    {
        static void Prefix(Quest __instance)
        {
            if (__instance.CurrentState == Quest.QuestState.Failed) return;
            if (__instance.CurrentState == Quest.QuestState.Completed) return;
            Log.Out("============================================");
            Log.Out("Starting quest {2} with phase {0} ({1}) => {3}",
                __instance.CurrentPhase, __instance.Position,
                __instance.QuestCode, __instance.CurrentState);
            if (__instance.PositionData.Count == 0)
                Log.Out("  no positional data yet!");
            foreach (var pos in __instance.PositionData)
                Log.Out("  pos {0} => {1}", pos.Key, pos.Value);
            foreach (var data in __instance.DataVariables)
                Log.Out("  data {0} => {1}", data.Key, data.Value);
            Log.Out("============================================");
        }
        static void Postfix(Quest __instance)
        {
            if (__instance.CurrentState == Quest.QuestState.Failed) return;
            if (__instance.CurrentState == Quest.QuestState.Completed) return;
            Log.Out("============================================");
            Log.Out("Started quest {2} with phase {0} ({1}) => {3}",
                __instance.CurrentPhase, __instance.Position,
                __instance.QuestCode, __instance.CurrentState);
            if (__instance.PositionData.Count == 0)
                Log.Out("  no positional data yet!");
            foreach (var pos in __instance.PositionData)
                Log.Out("  pos {0} => {1}", pos.Key, pos.Value);
            foreach (var data in __instance.DataVariables)
                Log.Out("  data {0} => {1}", data.Key, data.Value);
            Log.Out("============================================");
        }
    }

}

#endif