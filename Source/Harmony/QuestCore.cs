using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
public class OcbQuestCore : IModApi
{

    //########################################################
    //########################################################

    public static readonly HashSet<string>
        MailQuestPrefabs = new HashSet<string>();

    public const Quest.PositionDataTypes
        QuestStartPosition = (Quest.PositionDataTypes)254;
    public const Quest.PositionDataTypes
        QuestNewPosition = (Quest.PositionDataTypes)255;
    public const Quest.PositionDataTypes
        QuestPlanePosition = (Quest.PositionDataTypes)51;
    public const Quest.PositionDataTypes
        QuestCratePosition = (Quest.PositionDataTypes)52;

    public void InitMod(Mod mod)
    {
        Log.Out("OCB Harmony Patch: " + GetType().ToString());
        Harmony harmony = new Harmony(GetType().ToString());
        harmony.PatchAll(Assembly.GetExecutingAssembly());
    }

    //########################################################
    //########################################################

}
