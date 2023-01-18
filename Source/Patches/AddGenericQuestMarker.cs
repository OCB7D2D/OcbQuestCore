using HarmonyLib;
using System.Reflection;
using UnityEngine;

//###########################################################################
//###########################################################################

class AddGenericQuestMarker
{

    //#####################################################
    // Extend private "Quest.GetVariableText"
    // Note that we ignore the index (for now)
    //#####################################################


    [HarmonyPatch(typeof(Quest), "HandleMapObject")]
    public class QuestHandleMapObjectPatch
    {

        private static readonly MethodInfo MethodSetMapObjectSelected =
            AccessTools.Method(typeof(Quest), "SetMapObjectSelected");

        static bool Prefix(Quest __instance,
            Quest.PositionDataTypes dataType,
            string navObjectName,
            // int defaultTreasureRadius,
            MapObject ___mapObject,
            bool ___tracked)
        {
            if (dataType <= Quest.PositionDataTypes.TraderPosition) return true;
            if (__instance.OwnerJournal == null || navObjectName == "") return true;
            __instance.RemoveMapObject();
            if (__instance.GetPositionData(out var position, dataType))
            {
                var world = GameManager.Instance.World;
                if (___mapObject != null) world.ObjectOnMapRemove(
                        ___mapObject.type, (int)___mapObject.key);
                // Logic copied from vanilla
                if (navObjectName == "")
                {
                    ___mapObject = new MapObjectQuest(position);
                    world.ObjectOnMapAdd(___mapObject);
                }
                else
                {
                    __instance.NavObject = NavObjectManager.Instance.RegisterNavObject(
                        navObjectName, __instance.Position + new Vector3(0.5f, 0.5f, 0.5f));
                    __instance.NavObject.name = __instance.QuestClass.Name;
                    __instance.NavObject.ExtraData = 1f; // E.g. Treasure Radius
                    __instance.NavObject.IsActive = false;
                }
                MethodSetMapObjectSelected.Invoke(
                    __instance, new object[] { ___tracked });
                return false;
            }
            return true;
        }
    }

    //#####################################################
    //#####################################################

}

