using HarmonyLib;

//###########################################################################
// Hardcoded patch to let entites attack our supply crates.
// Doing this in XPath should be possible, but is pretty hairy.
// Note: this requires `FixEntityTargetFromMods` patch to work!
//###########################################################################

public class AddSupplyCrateToEaiTargets
{

    [HarmonyPatch(typeof(EAIApproachAndAttackTarget), "SetData")]
    public class EAIApproachAndAttackTargetSetDataPatch
    {

        //#####################################################
        //#####################################################

        static void Prefix(DictionarySave<string, string> data)
        {
            if (!data.TryGetValue("class", out string str)) return;
            if (str.Contains("EntityPlayer") == false) return;
            data["class"] = $"EntityAirDrop:QuestCore,0,{str}";
        }

        //#####################################################
        //#####################################################

    }
}
