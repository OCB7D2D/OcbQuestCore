using HarmonyLib;

//###########################################################################
// EAI will reset the attack target to player even though our spawner
// has explicitly set the target to be the AirDrop entity. This patch
// ensures that target is not overwritten by EAI until time is up.
//###########################################################################

public class KeepAiTargetFixedOnAirDrop
{

    //#####################################################
    //#####################################################

    [HarmonyPatch(typeof(EAISetNearestEntityAsTarget), "CanExecute")]
    private class EAISetNearestEntityAsTargetCanExecute
    {
        static readonly HarmonyFieldProxy<int> FieldAttackTargetTime =
            new HarmonyFieldProxy<int>(typeof(EntityAlive), "attackTargetTime");
        static bool Prefix(EAISetNearestEntityAsTarget __instance)
        {
            if (__instance.theEntity == null) return true;
            // Skip (return false) if time is valid and target is air drop
            return !(FieldAttackTargetTime.Get(__instance.theEntity) > 0
                && __instance.theEntity.GetAttackTarget() is EntityAirDrop);
        }
    }

    //#####################################################
    //#####################################################

}
