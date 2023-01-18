using UnityEngine;

//###########################################################################
//###########################################################################

public abstract class BaseObjectiveAirDrop : BaseCustomObjective
{

    //#####################################################
    //#####################################################

    protected EntityAirDrop EntityCrate = null;

    //#####################################################
    //#####################################################

    protected override bool useUpdateLoop => true;

    //#####################################################
    //#####################################################

    public static ObjectiveAirDrop GetLastAirDropObjective(Quest quest, BaseObjective objective)
    {
        for (int idx = quest.GetObjectiveIndex(objective) - 1; idx != -1; idx -= 1)
            if (quest.Objectives[idx] is ObjectiveAirDrop drop) return drop;
        return null;
    }

    //#####################################################
    //#####################################################

    public EntityAirDrop GetLastAirDropEntity()
    {
        if (OwnerQuest == null || EntityCrate != null) return EntityCrate;
        if (OwnerQuest.CurrentState == Quest.QuestState.Failed) return null;
        var drop = GetLastAirDropObjective(OwnerQuest, this);
        EntityCrate = drop?.FetchCrateEntity();
        if (EntityCrate == null) OwnerQuest.MarkFailed();
        return EntityCrate;
    }

    //#####################################################
    //#####################################################

    private float UpdateTime;

    public override void Update(float dt)
    {
        // if (Complete) return;
        if (UpdateTime >= Time.time) return;
        if (Complete) UpdateTime = Time.time + 2.5f;
        else UpdateTime = Time.time + 0.125f;
        base.Update(dt);
        Refresh();
    }

    protected abstract void UpdateCompletion();

    public override void Refresh()
    {
        GetLastAirDropEntity();
        if (OwnerQuest == null) return;
        if (EntityCrate == null) return;
        UpdateCratePositionData(OwnerQuest, EntityCrate);
        if (Complete) return;
        if (ObjectiveState == ObjectiveStates.NotStarted &&
            OwnerQuest.CurrentState == Quest.QuestState.InProgress) return;
        UpdateCompletion(); // Objectives may differ here
        if (Complete) OwnerQuest.CheckForCompletion();
    }

    //#####################################################
    //#####################################################

    static public void UpdateCratePositionData(Quest quest, EntityAirDrop crate)
    {
        if (crate == null) return;
        // Must set position data in order as we set the nav object to track it
        quest?.SetPositionData(OcbQuestCore.QuestCratePosition, crate.position);
        //OwnerQuest?.HandleMapObject(CratePoint, CrateNavObject);
        //if (quest?.NavObject?.TrackedPosition != null)
        //    if (quest.NavObject.TrackedPosition != crate.position)
        //        quest.NavObject.TrackedPosition = crate.position;
    }

    //#####################################################
    //#####################################################

}

