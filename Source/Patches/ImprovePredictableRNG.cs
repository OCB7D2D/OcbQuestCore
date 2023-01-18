using HarmonyLib;
using UnityEngine;

//###########################################################################
// Vanilla game will always re-use the same RNG seed once the game is
// loaded, meaning you will get the same loot again after each map load.
// ToDo: need to check if this is still the case with A21 update!?
//###########################################################################

public class ImprovePredictableRNG
{

    [HarmonyPatch(typeof(GameRandomManager), "SetBaseSeed")]
    public class GameRandomManagerSetBaseSeedPatch
    {

        //#####################################################
        //#####################################################

        static ulong counter = 2654435769;

        static void Prefix(ref int _baseSeed)
        {
            _baseSeed ^= Time.time.GetHashCode();
            _baseSeed ^= (counter++).GetHashCode();
            if (GameManager.Instance?.World == null) return;
            World world = GameManager.Instance.World;
            _baseSeed ^= world.worldTime.GetHashCode();
        }

        //#####################################################
        //#####################################################

    }

}
