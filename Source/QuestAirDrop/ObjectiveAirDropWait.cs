//###########################################################################
// This objective will wait for the air drop crate to land.
// It requires a previous AirDrop objective spawning the crate.
// You also have to complete that objective early via min-height.
// Usefull to have events in between spawn of crate and landing.
// Basically re-uses midair crate to check survival and landing.
//###########################################################################

public class ObjectiveAirDropWait : BaseObjectiveAirDrop
{

    //#####################################################
    // Implement the complete condition
    //#####################################################

    protected override void UpdateCompletion()
    {
        Complete = EntityCrate.onGround;
    }

    //#####################################################
    // Change text to "wait for landing"
    //#####################################################

    public override void SetupDisplay()
    {
        Description = Localization.Get("ObjectiveWaitForTouchDown");
        StatusText = string.Empty; // Reset the status text
    }

    //#####################################################
    // Implementation to allow cloning
    //#####################################################

    public override BaseObjective Clone()
    {
        var copy = new ObjectiveAirDropWait();
        CopyTo(copy);
        return copy;
    }

    //#####################################################
    //#####################################################

}
