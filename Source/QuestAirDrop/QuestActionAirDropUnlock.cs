//###########################################################################
// Action to unlock crate from previous `ObjectiveAirDrop`
//###########################################################################

public class QuestActionAirDropUnlock : BaseQuestAction
{

    //#####################################################
    // Unlock previous air-drop container
    //#####################################################

    public override void PerformAction(Quest ownerQuest)
    {
        // Check all objectives to find last/current air drops
        for (int i = OwnerQuest.Objectives.Count - 1; i != -1; i -= 1)
        {
            BaseObjective objective = OwnerQuest.Objectives[i];
            if (objective.Phase >= Phase) continue;
            if (!(objective is ObjectiveAirDrop drop)) continue;
            if (drop.FetchCrateEntity() != null)
                drop.FetchCrateEntity().IsReady = true;
            break; // Only unlock one container
        }
    }

    //#####################################################
    // Ensure objective can be cloned correctly
    //#####################################################

    public override BaseQuestAction Clone()
    {
        QuestActionAirDropUnlock clone =
            new QuestActionAirDropUnlock();
        CopyValues(clone);
        return clone;
    }

    //#####################################################
    //#####################################################

}
