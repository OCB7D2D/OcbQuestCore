using System.IO;
using UnityEngine;

//###########################################################################
// Mostly copied fom vanilla in order to support dynamic quests.
// Good example what is needed to convert an existing objective.
//###########################################################################

public class ObjectiveCountDown : BaseCustomObjective
{

    private float CurrentTime;
    // public static string PropTime = "time";
    // private int dayLengthInSeconds;

    protected string PropLabel = "ObjectiveTime_keyword";

    protected bool FailWhenZero = false;

    public override ObjectiveValueTypes ObjectiveValueType => ObjectiveValueTypes.Time;

    public override bool UpdateUI => true;

    public override string StatusText
    {
        get
        {
            if (CurrentTime <= 0.0)
            {
                if (ObjectiveState == ObjectiveStates.Complete)
                    return Localization.Get("completed");
                if (ObjectiveState == ObjectiveStates.Failed)
                    return Localization.Get("failed");
            }
            return XUiM_PlayerBuffs.GetTimeString(CurrentTime);
        }
    }

    public override void SetupObjective()
    {
        base.SetupObjective();
        keyword = Localization.Get(PropLabel);
        if (CurrentValue == 1) return;
        if (Value.EqualsCaseInsensitive("day")) CurrentTime =
            GamePrefs.GetInt(EnumGamePrefs.DayNightLength) * 60;
        else CurrentTime = StringParsers.ParseFloat(Value);
    }

    public override void SetupDisplay() => Description = string.Format("{0}:", keyword);

    public override void AddHooks() => QuestEventManager.Current.AddObjectiveToBeUpdated(this);

    public override void RemoveHooks() => QuestEventManager.Current.RemoveObjectiveToBeUpdated(this);

    public override void Refresh()
    {
        SetupDisplay();
        // Fails quest if phase doesn't progress until timer runs up?
        if (FailWhenZero)
        {
            if (CurrentTime <= 0.0f)
            {
                // `Complete=false` will "not work" due to setter!
                // Once it is set true, it always returns true!!
                ObjectiveState = ObjectiveStates.Failed;
                if (!Optional) OwnerQuest.MarkFailed();
            }
            else
            {
                // Mark objective as ready to complete
                // This allows quest phase to progress
                Complete = true;
            }
        }
        // Or fail the quest once the timer runs up?
        else
        {
            if (Optional) Complete = CurrentTime > 0.0f;
            else Complete = CurrentTime <= 0.0f;
        }
        // Check if phase is completed and ready to move on
        if (Complete) OwnerQuest.CheckForCompletion();
    }

    public override void Read(BinaryReader _br)
    {
        CurrentTime = _br.ReadUInt16();
        currentValue = 1;
    }

    public override void Write(BinaryWriter _bw) =>
        _bw.Write((ushort)CurrentTime);

    //#####################################################
    // Implementation to allow cloning
    //#####################################################

    protected void CopyTo(ObjectiveCountDown into)
    {
        into.TickInterval = TickInterval;
        into.FailWhenZero = FailWhenZero;
        into.CurrentTime = CurrentTime;
        into.PropLabel = PropLabel;
        base.CopyTo(into);
    }

    public override BaseObjective Clone()
    {
        var copy = new ObjectiveCountDown();
        CopyTo(copy);
        return copy;
    }

    //#####################################################
    // Manual way of setting stuff via xml configs
    //#####################################################

    public override void ParseProperties(
        DynamicProperties properties)
    {
        base.ParseProperties(properties);
        if (properties.Values.ContainsKey("time"))
            Value = properties.Values["time"];
        if (properties.Values.ContainsKey("label"))
            PropLabel = properties.Values["label"];
        if (properties.Values.ContainsKey("fail_when_zero"))
            FailWhenZero = properties.GetBool("fail_when_zero");
    }

    // Use internally only
    protected float LastUpdateTime;

    // Adjust to speed up the UI updates
    public float TickInterval = 0.25f;

    public override void Update(float updateTime)
    {
        CurrentTime -= updateTime;
        if (Time.time <= LastUpdateTime) return;
        LastUpdateTime = Time.time + TickInterval;
        if (FailWhenZero) Refresh();
        if (CurrentTime > 0.0) return;
        HandleRemoveHooks();
        Refresh();
    }

    public override void HandleFailed()
    {
        CurrentTime = 0.0f;
        Complete = false;
    }


}
