using System.IO;
using UnityEngine;

//###########################################################################
//###########################################################################

public abstract class BaseCustomObjective : BaseObjective
{

    public bool IsVisible = true;

    //#####################################################
    // Some access helpers
    //#####################################################

    public EntityPlayerLocal Player =>
        OwnerQuest?.OwnerJournal?.OwnerPlayer;

    // The quest giver entity (normally as EntityNPC)
    public Entity QuestGiver => OwnerQuest.QuestGiverID == -1
        ? null : WRLD.GetEntity(OwnerQuest.QuestGiverID);

    // Get the main local world from GameManager
    // ToDo: maybe re-use once passed to us
    public static World WRLD => GameManager.Instance.World;

    // Get the main random number generator
    // ToDo: maybe re-use once passed to us
    public static GameRandom RNG => WRLD.GetGameRandom();

    //#####################################################
    // Implementation to allow cloning
    //#####################################################

    public void CopyTo(BaseCustomObjective into)
    {
        base.CopyValues(into);
        into.IsVisible = IsVisible;
    }

    //#####################################################
    // SetPosition is called on client side after the
    // list of quests is recieved (sets options back)
    //#####################################################
    // This is really a weird indirection and I have no
    // clue why TFP doesn't set the owner quest position
    // from `NetPackageNPCQuestList.QuestPacketEntry`!?
    // Might have better been called `SetQuestLocation`
    //#####################################################

    public override void SetPosition(Vector3 position, Vector3 size)
    {
        OwnerQuest.SetPositionData(Quest.PositionDataTypes.
            Location, OwnerQuest.Position = position);
        // Also set the position for our next position anchor
        // This way we can consume position once and only once
        OwnerQuest.SetPositionData(OcbQuestCore.QuestNewPosition, position);
    }

    //#####################################################
    // SetupObjective is exactly called once when the
    // quest is started (or restarted after loading).
    //#####################################################

    public override void SetupObjective()
    {
        // Mark as complete if from previous phase
        // Fixes minor issue when loading started quest
        if (Phase < OwnerQuest.CurrentPhase)
            ObjectiveState = ObjectiveStates.Complete;
        if (OwnerQuest?.Position == Vector3.zero)
        {
            if (OwnerQuest.GetPositionData(out var position,
                Quest.PositionDataTypes.Location))
                    OwnerQuest.Position = position;
            if (OwnerQuest?.Position != Vector3.zero)
            {
                Log.Out("Setup quest position via setup objective to {0}", OwnerQuest.Position);
            }
            else if (OwnerQuest.CurrentState == Quest.QuestState.Failed) { }
            else if (OwnerQuest.CurrentState == Quest.QuestState.Completed) { }
            else Log.Out("Setup quest had no data in position yet; ask server!?");

        }
        base.SetupObjective();
        // OnValueChanged();
        // SetupDisplay();
    }

    // public virtual void SetPosition(Vector3 position)
    // {
    //     MarkTargetPosActive(position);
    // }

    // Specialized implemetation on `ObjectiveAirDrop`
    // To re-aquire position after a quest "restart" on world load
    protected virtual void ObjectiveActivated(bool loaded = false)
    {
    }

    public override void AddHooks()
    {
        // One of the most important hooks for us!
        ObjectiveActivated(CurrentValue == 0);
        ValueChanged += OnValueChanged;
        base.AddHooks();
    }

    public override void RemoveHooks()
    {
        ValueChanged -= OnValueChanged;
        base.RemoveHooks();
    }

    protected virtual void OnValueChanged()
    {
    }

    //#####################################################
    // Manual way of setting stuff via xml configs
    //#####################################################

    public override void ParseProperties(DynamicProperties properties)
    {
        // Parses `ID`, `Value`, `Phase` etc
        base.ParseProperties(properties);
        IsVisible = !HiddenObjective;
        // Additionally we also support distance
        if (properties.Values.ContainsKey("visible"))
            IsVisible = properties.GetBool("visible");
    }

    //#####################################################
    // Persist all settings in player data if required.
    // We need to store all data in the save file for
    // all objectives that are created programmatically.
    // Those can't be re-read from xml config, so we need
    // to persist evertything important in the save file.
    //#####################################################

    public virtual void ReadConfig(BinaryReader _br)
    {
        base.Read(_br);
        Phase = _br.ReadByte();
        ID = _br.ReadString();
        Value = _br.ReadString();
        NavObjectName = _br.ReadString();
        HiddenObjective = _br.ReadBoolean();
        Optional = _br.ReadBoolean();
        Complete = _br.ReadBoolean();
        IsVisible = _br.ReadBoolean();
        ObjectiveState = (ObjectiveStates)_br.ReadByte();
    }

    public virtual void WriteConfig(BinaryWriter _bw)
    {
        base.Write(_bw);
        _bw.Write(Phase);
        _bw.Write(ID ?? string.Empty);
        _bw.Write(Value ?? string.Empty);
        _bw.Write(NavObjectName ?? string.Empty);
        _bw.Write(HiddenObjective);
        _bw.Write(Optional);
        _bw.Write(Complete);
        _bw.Write(IsVisible);
        _bw.Write((byte)ObjectiveState);
    }

    //#####################################################
    //#####################################################

}

//###########################################################################
//###########################################################################
