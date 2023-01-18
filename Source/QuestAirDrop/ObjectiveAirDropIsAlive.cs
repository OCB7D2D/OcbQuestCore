//###########################################################################
// This objective will fail once the crate from the 
// required previous AirDrop objective is killed.
//###########################################################################

public class ObjectiveAirDropIsAlive : BaseObjectiveAirDrop
{

    //#####################################################
    // Implement the complete condition
    //#####################################################

    protected override void UpdateCompletion()
    {
        Complete = OwnerQuest.CurrentState != Quest.QuestState.Failed;
    }

    //#####################################################
    // Change text to "keep air-drop alive"
    //#####################################################

    public override void SetupDisplay()
    {
        Description = Localization.Get("ObjectiveKeepAirDropAlive");
        StatusText = string.Empty; // Reset the status text
    }

    //#####################################################
    // Implementation to allow cloning
    //#####################################################

    public override BaseObjective Clone()
    {
        var copy = new ObjectiveAirDropIsAlive();
        CopyTo(copy);
        return copy;
    }

    //#####################################################
    //#####################################################

}
