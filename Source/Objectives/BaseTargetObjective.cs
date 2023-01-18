using System.IO;
using UnityEngine;

//###########################################################################
// Base objective to reach a certain point, same as `ObjectiveRandomGoto`.
// We re-implement most of it, since it isn't too much, and for more control.
// We also persist all of our config, since we want to part of my new custom
// and dynamic quest system, which means that config options are given and
// set dynamically, thus we can't simply read them back from the xml config.
//###########################################################################

public abstract class BaseTargetObjective : BaseCustomObjective
{

    //#####################################################
    // Options to configure also during run-time
    //#####################################################

    // The position to reach
    public Vector3 TargetPos;

    //public virtual void SetPosition(Vector3 position)
    //{
    //    if (position == Vector3.zero) return;
    //    MarkTargetPosActive(position);
    //}

    //#####################################################
    //#####################################################

    protected virtual bool CreateQuestPosition => false;

    protected virtual void FetchAsyncStartPosition()
    {
        // CurrentValue = 2;
    }

    protected virtual string GetKeyword() =>
        Localization.Get("ObjectiveRallyPointHeadTo");

    protected override void OnValueChanged()
    {
        if (ObjectiveState == ObjectiveStates.Failed) keyword = Localization.Get("failed");
        else if (ObjectiveState == ObjectiveStates.Complete) keyword = Localization.Get("completed");
        // else if (ObjectiveState == ObjectiveStates.NotStarted) keyword = Localization.Get("ObjectiveWaitForStart");
        else if (CurrentValue == 0) keyword = Localization.Get("ObjectiveWaitForStart");
        else if (CurrentValue == 1) keyword = Localization.Get("ObjectiveWaitForServer");
        else keyword = GetKeyword();
        base.OnValueChanged();
        // Display might depend on keyword
        // Which is updated on value change
        SetupDisplay();
    }

    protected override void ObjectiveActivated(bool loaded = false)
    {
        // Reset waiting for server after restarts
        if (CurrentValue == 1) CurrentValue = 0;
        // To early here to request position from server
        // Reason being this quest doesn't have a valid code yet
        // And without it, the answer would not find the quest again
        // But when loaded from save, we need to re-use a position!?
        if (CurrentValue >= 2)
        {
            if (OwnerQuest.PositionData.TryGetValue(
                    OcbQuestCore.QuestNewPosition,
                    out Vector3 position))
            {
                MarkTargetPosActive(position);
            }
            // Otherwise the quest might come from a trader
            // In that case we take the position of the location
            else if (OwnerQuest.PositionData.TryGetValue(
                                Quest.PositionDataTypes.Location,
                                out Vector3 location))
            {
                MarkTargetPosActive(location);
            }
            else
            {
                Log.Warning("No next position to activate objective");
            }
        }
    }

    public override void Update(float deltaTime)
    {
        if (CurrentValue == 0)
        {
            // Check if we should re-use the initial position from trader
            // Don't really like the hardcoded "Phase == 1" part, but works
            if (Phase == 1 && OwnerQuest.Position != Vector3.zero)
            {
                // Will update state to `2`
                MarkTargetPosActive(OwnerQuest.Position);
            }
            else if (CreateQuestPosition)
            {
                // Log.Out("Create new position? {0}", (CreateQuestPosition));
                // Log.Out("Quest.Pos {0}", OwnerQuest.Position);
                OwnerQuest.PositionData.Remove(
                    OcbQuestCore.QuestNewPosition);
                // Might update state to `1`
                FetchAsyncStartPosition();
            }
            else
            {
                Log.Error("How does this target gets its position?");
                Log.Out("  Quest.Pos {0}", OwnerQuest.Position);
            }
            if (CurrentValue == 0)
                CurrentValue = 2;
        }
        if (CurrentValue == 1)
        {
            Log.Out("Looking for QuestNewPosition");
            if (OwnerQuest.PositionData.TryGetValue(
                    OcbQuestCore.QuestNewPosition,
                    out Vector3 position))
            {
                // Will update state to `2`
                Log.Out("Got a new position");
                MarkTargetPosActive(position);
            }
        }
        base.Update(deltaTime);
    }

    //#####################################################
    // Helper to call once objective goes active
    //#####################################################

    protected virtual void MarkTargetPosActive(Vector3 position)
    {
        // Register us to be current quest position
        OwnerQuest.Position = TargetPos = position;
        // Register us as the main location and show the map marker
        OwnerQuest.SetPositionData(Quest.PositionDataTypes.Location, TargetPos);
        // OwnerQuest.NavObject.TrackedPosition = position;
        OwnerQuest.HandleMapObject(Quest.PositionDataTypes.Location, NavObjectName);
        // Quest is fully set up
        CurrentValue = 2;
        // Refresh the current quest once position is recieved
        // OwnerQuest.OwnerJournal?.RefreshQuest(OwnerQuest);
    }

    //#####################################################
    // Implementation to allow cloning
    //#####################################################

    public void CopyTo(BaseTargetObjective into)
    {
        base.CopyTo(into);
        into.TargetPos = TargetPos;
    }

    //#####################################################
    // Main function to check for progress
    //#####################################################

    public override void Refresh()
    {
        if (Complete = CurrentValue == 3) OwnerQuest.CheckForCompletion(
            playObjectiveComplete: PlayObjectiveComplete);
    }

    //#####################################################
    // Implementation to support variable replacement
    // in dialogs and other situration (popups etc.)
    //#####################################################

    // Only used by `Quest.GetVariableText`
    public override string ParseBinding(string bindingName)
    {
        if (bindingName == "distance")
        {
            if (QuestGiver is EntityNPC trader)
            {
                var distance = Vector3.Distance(trader.position, TargetPos);
                return ValueDisplayFormatters.Distance(distance);
            }
        }
        else if (bindingName == "direction")
        {
            if (QuestGiver is EntityNPC trader)
            {
                Vector2 target = new Vector2(TargetPos.x, TargetPos.z);
                Vector2 start = new Vector2(trader.position.x, trader.position.z);
                var direction = GameUtils.GetDirByNormal(target - start);
                return ValueDisplayFormatters.Direction(direction);
            }
        }
        // Fall back to our base implementation
        return base.ParseBinding(bindingName);
    }

    //#####################################################
    // Persist settings in player data  
    // We need to store all data in the save file, since
    // we may have objectives created programmatically.
    // Those can't be re-read from xml config, so we need
    // to persist evertything important in the save file.
    //#####################################################

    public override void ReadConfig(BinaryReader _br)
    {
        base.ReadConfig(_br);
        // TargetPos = StreamUtils.ReadVector3(_br);
    }

    public override void WriteConfig(BinaryWriter _bw)
    {
        base.WriteConfig(_bw);
        // StreamUtils.Write(_bw, TargetPos);
    }

    //#####################################################
    //#####################################################

}
