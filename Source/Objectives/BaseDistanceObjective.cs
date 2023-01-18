using System.IO;
using UnityEngine;

//###########################################################################
// Base objective to reach a certain point, same as `ObjectiveRandomGoto`.
// We re-implement most of it, since it isn't too much, and for more control.
// We also persist all of our config, since we want to part of my new custom
// and dynamic quest system, which means that config options are given and
// set dynamically, thus we can't simply read them back from the xml config.
//###########################################################################

public abstract class BaseDistanceObjective : BaseTargetObjective
{

    //#####################################################
    // Options to configure also during run-time
    //#####################################################

    // Stay completed after you reached it once
    public bool OnlyReachOnce = true;

    // Distance to reach for objective to complete
    public float CompletionDistance = 10f;

    // Adjust to speed up the UI updates
    public float TickInterval = 1f;

    //#####################################################
    // Some overloads to set base class behaviour
    //#####################################################

    // Stop to call update loop once we reached level 3 (if option is set)
    protected override bool useUpdateLoop => true; // CurrentValue != 3 || !OnlyReachOnce;

    // Update the UI until the objective is Failed
    public override bool UpdateUI => ObjectiveState != ObjectiveStates.Failed;

    // Set the type of the objective value (not really used, beside bool in one case)
    public override ObjectiveValueTypes ObjectiveValueType => ObjectiveValueTypes.Distance;

    //#####################################################
    // Internal variables
    //#####################################################

    // Used in StatusText (and Update)
    protected float Distance = -2f;

    // Use internally only
    protected float LastUpdateTime;

    //#####################################################
    // Implementation to allow cloning
    //#####################################################

    public void CopyTo(BaseDistanceObjective into)
    {
        base.CopyTo(into);
        // Copy values from parent class
        // It doesn't implement copy itself
        into.CompletionDistance = CompletionDistance;
        into.OnlyReachOnce = OnlyReachOnce;
        into.TickInterval = TickInterval;
        // into.positionSet = positionSet;
        // into.TargetPos = TargetPos;
    }

    //#####################################################
    // Manual way of setting stuff via xml configs
    //#####################################################

    public override void ParseProperties(DynamicProperties properties)
    {
        // Parses `ID`, `Value`, `Phase` etc
        base.ParseProperties(properties);
        // Additionally we also support distance
        if (properties.Values.ContainsKey("distance"))
            Value = properties.Values["distance"];
        if (properties.Values.ContainsKey("completion_distance"))
            CompletionDistance = StringParsers.ParseFloat(
                properties.Values["completion_distance"]);
    }

    //#####################################################
    //#####################################################

    // Called on `StartQuest`
    // Also `SetupSharedQuest`
    // Assumed to be called once only
    public override void SetupObjective()
    {
        base.SetupObjective();
        // Not sure if keyword has any actual semantics
        // It just seems to be a shared string and re-used
        keyword = Localization.Get("ObjectiveRallyPointHeadTo");
        OwnerQuest.HandleMapObject(Quest.PositionDataTypes.Location, NavObjectName);
    }

    // This is also called in `Refresh`
    public override void SetupDisplay()
    {
        Description = keyword;
        // StatusText = "";
    }

    //#####################################################
    // Implementation to handle on/off calls
    //#####################################################

    //public override void AddHooks()
    //{
    //    // QuestEventManager.Current.AddObjectiveToBeUpdated(this);
    //   //OwnerQuest.HandleMapObject(Quest.PositionDataTypes.Location, NavObjectName);
    //    base.AddHooks(); // Does nothing
    //}
    //
    //public override void RemoveHooks()
    //{
    //    // QuestEventManager.Current.RemoveObjectiveToBeUpdated(this);
    //    base.RemoveHooks(); // Does nothing
    //}

    //#####################################################
    // Main function called to periodically check
    // if state has changed (e.g. reached area)
    //#####################################################

    protected override void MarkTargetPosActive(Vector3 position)
    {
        if (Player) Distance = Vector3.Distance(Player.position, position);
        base.MarkTargetPosActive(position);
    }

    // Update is only executed on the client
    public override void Update(float deltaTime)
    {
        // De-Bounce according to `TickInterval`
        if (Time.time <= LastUpdateTime) return;
        LastUpdateTime = Time.time + TickInterval;

        if (CurrentValue < 2)
        {
            base.Update(deltaTime);
        }
        else if (CurrentValue < 4)
        {
            // Update the distance (to be used by `StatusText`)
            Distance = Vector3.Distance(Player.position, TargetPos);
            if (CurrentValue == 2)
            {
                // Skip progress if still too far away
                if (Distance > CompletionDistance) return;
                // Check additional quest requirements
                if (!OwnerQuest.CheckRequirements()) return;
                // Disable the update function when reached
                if (OnlyReachOnce) QuestEventManager.Current
                        .RemoveObjectiveToBeUpdated(this);
                // Progress to next state
                CurrentValue = 3;
            }
            else
            {
                // Keep in complete state if still within distance
                if (Distance < CompletionDistance) return;
                // Regress to previous state
                CurrentValue = 2;
            }
            // Refresh
            Refresh();

        }

    }

    //#####################################################
    // Main function to check for progress
    //#####################################################

    protected virtual byte LastValue => 3;

    public override void Refresh()
    {
        // Change base method to check for variable max last value
        if (Complete = CurrentValue == LastValue) OwnerQuest.CheckForCompletion(
            playObjectiveComplete: PlayObjectiveComplete);
    }

    //#####################################################
    // Status text to display after the objective text
    //#####################################################

    // Called via various `Objective.GetBindingValue`
    public override string StatusText =>
        ObjectiveState == ObjectiveStates.Complete ? Localization.Get("completed")
        : ObjectiveState == ObjectiveStates.Failed ? Localization.Get("failed")
        : ObjectiveState == ObjectiveStates.NotStarted ? Localization.Get("waiting")
        : CurrentValue == 0 ? Localization.Get("ObjectiveWaitForStart")
        : CurrentValue == 1 ? Localization.Get("ObjectiveWaitForServer")
        : ValueDisplayFormatters.Distance(Distance);

    //#####################################################
    //#####################################################

    protected void UpdateDistanceToEntity(Entity player, Entity entity)
    {
        if (entity == null) Distance = 9999f;
        else if (player == null) Distance = 9999f;
        else Distance = Vector3.Distance(
            entity.position, player.position);
    }

    protected void UpdateDistanceFromPlayer(Entity entity)
    {
        if (entity == null) Distance = 9999f;
        else Distance = Vector3.Distance(
            entity.position, TargetPos);
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

    public override void Read(BinaryReader _br)
    {
        base.Read(_br);
        // TickInterval = _br.ReadSingle();
        // OnlyReachOnce = _br.ReadBoolean();
        // CompletionDistance = _br.ReadSingle();
        // TargetPos = StreamUtils.ReadVector3(_br);
    }

    public override void Write(BinaryWriter _bw)
    {
        base.Write(_bw);
        // _bw.Write(TickInterval);
        // _bw.Write(OnlyReachOnce);
        // _bw.Write(CompletionDistance);
        // StreamUtils.Write(_bw, TargetPos);
    }

    //#####################################################
    //#####################################################

}
