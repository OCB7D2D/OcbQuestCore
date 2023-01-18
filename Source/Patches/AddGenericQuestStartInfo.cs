using HarmonyLib;
using UnityEngine;

//###########################################################################
// Fix issue that it is not easy to show a start position/direction for
// offered quests, if you don't use the built in types. Although those
// provide more detailed information, a generic approach is often good
// enough for most use cases (e.g. for going to a random place first).
//###########################################################################

class AddGenericQuestStartInfo
{

    //#####################################################
    // Extend private "Quest.GetVariableText"
    // Note that we ignore the index (for now)
    //#####################################################

    [HarmonyPatch(typeof(Quest), "GetVariableText")]
    public class QuestGetVariableText
    {
        static bool Prefix(Quest __instance, string field,
            int index, string variableName, ref string __result)
        {
            //****************************************
            //****************************************
            if (field == "start")
            {
                // Use main quest position
                Vector3 position = __instance.Position;
                // Or if unset, try to fetch
                if (position == Vector3.zero)
                {
                    // Fetch position from location data
                    if (__instance.GetPositionData(out position,
                        Quest.PositionDataTypes.Location))
                    {
                        __instance.Position = position;
                    }
                }
                // Return info for start position
                switch (variableName)
                {
                    case "distance":
                        if (__instance.QuestGiverID != -1)
                        {
                            if (GameManager.Instance.World.GetEntity(__instance.QuestGiverID) is EntityNPC entity)
                            {
                                var distance = Vector3.Distance(entity.position, __instance.Position);
                                __result = ValueDisplayFormatters.Distance(distance);
                                return false;
                            }
                        }
                        break;
                    case "direction":
                        if (__instance.QuestGiverID != -1)
                        {
                            if (GameManager.Instance.World.GetEntity(__instance.QuestGiverID) is EntityNPC entity)
                            {
                                Vector2 start = new Vector2(entity.position.x, entity.position.z);
                                Vector2 target = new Vector2(__instance.Position.x, __instance.Position.z);
                                __result = ValueDisplayFormatters.Direction(GameUtils.GetDirByNormal(target - start));
                                return false;
                            }
                        }
                        break;
                }
                __result = field;
                return false;
            }
            //****************************************
            //****************************************
            else if (field == "quest")
            {
                // Return info for start position
                switch (variableName)
                {
                    case "tier":
                        __result = __instance.QuestClass.DifficultyTier.ToString();
                        return false;
                }
                __result = field;
                return false;
            }
            //****************************************
            //****************************************
            // ToDo: So far no implementation for Custom Quests
            // if (!(__instance is QuestCustom quest)) return true;
            // __result = __instance.GetVariableText(field, index, variableName);
            // return string.IsNullOrEmpty(__result);
            //****************************************
            //****************************************
            return true;
        }
    }

    //#####################################################
    //#####################################################

}
