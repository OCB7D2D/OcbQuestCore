using Audio;
using UnityEngine;

//###########################################################################
// QuestEventManager.Current.QuestBounds = new Rect();
//###########################################################################

public class ObjectiveStayClose : BaseCustomObjective
{


    //#####################################################
    // Options to configure also during run-time
    //#####################################################

    private float Radius = 50f;
    private string RadiusCached = "0 km";
    public static string PropRadius = "radius";
    protected string PropLabel = "ObjectiveStayWithin_keyword";

    //#####################################################
    // Some overloads to set base class behaviour
    //#####################################################

    protected override bool useUpdateLoop => true;

    // Update the UI until the objective is Failed
    public override bool UpdateUI => ObjectiveState != ObjectiveStates.Failed;

    // Set the type of the objective value (not really used, beside bool in one case)
    public override ObjectiveValueTypes ObjectiveValueType => ObjectiveValueTypes.Distance;
    
    //#####################################################
    // Internal variables
    //#####################################################

    private float CurrentDistance;
    //private bool PositionSetup;

    private bool HasReachedOnce = false;

    private string WarningSound = string.Empty;
    private bool WarningIsPlaying = false;

    //#####################################################
    // Implementation to allow cloning
    //#####################################################

    protected void CopyTo(ObjectiveStayClose into)
    {
        base.CopyTo(into);
        into.Radius = Radius;
        into.PropLabel = PropLabel;
        into.RadiusCached = RadiusCached;
        into.HasReachedOnce = HasReachedOnce;
        into.WarningSound = WarningSound;
    }

    public override BaseObjective Clone()
    {
        var copy = new ObjectiveStayClose();
        CopyTo(copy);
        return copy;
    }

    //#####################################################
    // Manual way of setting stuff via xml configs
    //#####################################################

    public override void ParseProperties(DynamicProperties properties)
    {
        base.ParseProperties(properties);
        if (properties.Values.ContainsKey("radius"))
            Radius = properties.GetFloat("radius");
        if (properties.Values.ContainsKey("label"))
            PropLabel = properties.Values["label"];
        if (properties.Values.ContainsKey("warn_sfx"))
            WarningSound = properties.Values["warn_sfx"];
        RadiusCached = ValueDisplayFormatters.Distance(Radius);
    }

    //#####################################################
    //#####################################################

    public override void SetupObjective()
    {
        keyword = Localization.Get(PropLabel);
        base.SetupObjective();
        Complete = true;
    }

    public override void SetupDisplay()
        => Description = keyword;

    //#####################################################
    //#####################################################

    public override string StatusText
    {
        get
        {
            if (OwnerQuest.CurrentState == Quest.QuestState.InProgress)
            {
                if (HasReachedOnce)
                {
                    if (CurrentDistance < Radius)
                    {
                        HasReachedOnce = true;
                        return string.Format("{0} ({1})", RadiusCached,
                            ValueDisplayFormatters.Distance(CurrentDistance));
                    }
                    else
                    {
                        ObjectiveState = ObjectiveStates.Failed;
                    }
                }
                else
                {
                    HasReachedOnce |= CurrentDistance < Radius;
                    ObjectiveState = ObjectiveStates.InProgress;
                    return RadiusCached;
                }
            }
            if (OwnerQuest.CurrentState == Quest.QuestState.NotStarted) return RadiusCached;
            if (ObjectiveState == ObjectiveStates.Failed) return Localization.Get("failed");
            return Localization.Get("completed");
        }
    }

    //#####################################################
    //#####################################################

    // public override void AddHooks() => QuestEventManager.Current.AddObjectiveToBeUpdated(this);

    // public override void RemoveHooks()
    // {
    //     QuestEventManager.Current.RemoveObjectiveToBeUpdated(this);
    // }

    public override void Refresh()
    {
        SetupDisplay();
        if (ObjectiveState == ObjectiveStates.NotStarted && OwnerQuest.CurrentState == Quest.QuestState.InProgress)
            return;
        Complete = OwnerQuest.CurrentState != Quest.QuestState.Failed;
    }

    //#####################################################
    //#####################################################

    private void PlayWarningSound(int entityId)
    {
        if (WarningIsPlaying == true) return;
        if (string.IsNullOrWhiteSpace(WarningSound)) return;
        Manager.PlayInsidePlayerHead(WarningSound, entityId, 0, true);
        WarningIsPlaying = true;
    }

    private void StopWarningSound(int entityId)
    {
        if (WarningIsPlaying == false) return;
        if (string.IsNullOrWhiteSpace(WarningSound)) return;
        Manager.StopLoopInsidePlayerHead(WarningSound, entityId);
        WarningIsPlaying = false;
    }

    //#####################################################
    //#####################################################

    public override void HandleCompleted()
    {
        base.HandleCompleted(); // NOP, still a keeper
        QuestJournal journal = OwnerQuest.OwnerJournal;
        EntityPlayerLocal player = journal.OwnerPlayer;
        StopWarningSound(player.entityId);
    }

    public override void HandleFailed()
    {
        base.HandleFailed(); // NOP, still a keeper
        QuestJournal journal = OwnerQuest.OwnerJournal;
        EntityPlayerLocal player = journal.OwnerPlayer;
        StopWarningSound(player.entityId);
    }

    public override void HandlePhaseCompleted()
    {
        base.HandlePhaseCompleted(); // NOP, still a keeper
        QuestJournal journal = OwnerQuest.OwnerJournal;
        EntityPlayerLocal player = journal.OwnerPlayer;
        StopWarningSound(player.entityId);
    }

    public override void RemoveHooks()
    {
        base.RemoveHooks(); // Actually does something ;)
        QuestJournal journal = OwnerQuest.OwnerJournal;
        EntityPlayerLocal player = journal.OwnerPlayer;
        StopWarningSound(player.entityId);
    }

    //#####################################################
    //#####################################################

    public override void Update(float updateTime)
    {
        QuestJournal journal = OwnerQuest.OwnerJournal;
        EntityPlayerLocal player = journal.OwnerPlayer;
        Vector3 position = player.position;
        Vector3 pos = OwnerQuest.Position;

        position.y = pos.y = 0.0f;
        CurrentDistance = (position - pos).magnitude;
        float factor = CurrentDistance / Radius;
        if (factor > 1.0f)
        {
            if (HasReachedOnce)
            {
                Complete = false;
                ObjectiveState = ObjectiveStates.Failed;
                StopWarningSound(player.entityId);
                OwnerQuest.MarkFailed();
            }
        }
        else
        {
            HasReachedOnce = true;
            if (factor > 0.75f)
            {
                ObjectiveState = ObjectiveStates.Warning;
                PlayWarningSound(player.entityId);
            }
            else
            {
                ObjectiveState = ObjectiveStates.Complete;
                StopWarningSound(player.entityId);
            }
        }
        // Refresh();
    }

    //#####################################################
    //#####################################################

}
