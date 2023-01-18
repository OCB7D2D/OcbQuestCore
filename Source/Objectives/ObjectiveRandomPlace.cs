
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

//###########################################################################
// This objective will choose a random place and set it as the main
// quest target, until it finds that another objective has already set
// `PositionData((Quest.PositionDataTypes)39)`, which is a "fake" data
// position, which is none the less attached to the main quest object.
// If we find this data position, we will create our TargetPos to be
// from that data position and then update it to our new `TargetPos`.
//###########################################################################
// Note that vanilla mainly stores the `CurrentValue` and we try to
// stay inside that semantics. All quests start with value `0` and
// a value of `1` means, we are waiting for a server answer. This
// should only be required on remote clients though. NPC quests are
// pre-initialized with a position (so the trader can tell where a
// quest starts). Therefore quests started from trader basically
// always have valid `OwnerQuest.Position` (the TurnIn types).
//###########################################################################

public class ObjectiveRandomPlace : BaseDistanceObjective
{

    //#####################################################
    //#####################################################

    public string KeywordProp = "HeadToPosition";

    //#####################################################
    //#####################################################

    public float MinDistance = 40f;
    public float MaxDistance = 60f;
    public bool RepeatPOI = true;
    public bool UpdateNPC = false;

    //#####################################################
    // Some overloads to set base class behaviour
    //#####################################################

    // Our objective creates a new (async) quest location
    protected override bool CreateQuestPosition => 
        Target != QuestTarget.Reuse;

    //#####################################################
    // Helper to call once objective goes active
    //#####################################################

    protected override void MarkTargetPosActive(Vector3 position)
    {
        base.MarkTargetPosActive(position);
        if (RepeatPOI == false)
        {
            string ID = "QuestFaction";
            string faction = OwnerQuest.QuestFaction.ToString();
            if (OwnerQuest.DataVariables.ContainsKey(ID))
                OwnerQuest.DataVariables[ID] = faction;
            else OwnerQuest.DataVariables.Add(ID, faction);
        }
    }

    //#####################################################
    // Called to setup initial owner quest position.
    // We are one objective under potentially many others!
    // Offered to all objectives, but first one wins.
    //#####################################################

    // Must only be called on the server
    // Seems to be the case with vanilla
    public override bool SetupPosition(
      EntityNPC trader = null,
      EntityPlayer player = null,
      List<Vector2> POICache = null,
      int entityIDforQuests = -1)
    {
        if (TargetPos != Vector3.zero)
            Log.Error("SetupPosition already executed!?");

        if (!ConnectionManager.Instance.IsServer)
            Log.Error("SetupPosition only allowed on server");

        Vector3 position = Vector3.zero;

#if DEBUG
        Log.Out("Execute SetupPosition from trader {0} ({1}-{2})",
            trader.position, MinDistance, MaxDistance);
        Log.Out("   Player {0}", Player?.position);
        Log.Out("   QuestGiver {0}", QuestGiver?.position);
        Log.Out("   QuestPosition {0}", OwnerQuest?.Position);
        Log.Out("   QuestStart {0}", OwnerQuest?.Position);
#endif

        if (Anchor == QuestAnchor.Player) position = Player.position;
        else if (Anchor == QuestAnchor.QuestGiver) position = QuestGiver?.position ?? Player.position;
        else if (Anchor == QuestAnchor.QuestPosition) position = OwnerQuest.Position;
        else if (Anchor == QuestAnchor.QuestStart) OwnerQuest.GetPositionData
                (out position, OcbQuestCore.QuestStartPosition);
        else if (Anchor == QuestAnchor.TraderPosition) OwnerQuest.GetPositionData
                (out position, Quest.PositionDataTypes.TraderPosition);
        else if (Target == QuestTarget.Reuse) Log.Error("Reuse should not call SetupPosition");

        var world = GameManager.Instance.World;
        byte faction = trader?.NPCInfo?.QuestFaction ?? OwnerQuest.QuestFaction;

        List<Tuple<byte, Vector3i>> updates = new List<Tuple<byte, Vector3i>>();
        if (position == Vector3.zero) position = Player.position;
        position = StartPosition.FindTargetPosition(ref updates, world, player, position,
            MinDistance, MaxDistance, Target, true, faction, 255);

        foreach (var item in updates)
        {
            OwnerQuest.SetPositionData((Quest.PositionDataTypes)item.Item1, item.Item2);
        }

        // Find or create position on server
        // Vector3 position = FindLocation(WRLD, RNG, trader.position);
        if (position == Vector3.zero) return false;
        MarkTargetPosActive(position);
        return true;
    }

    public override void HandleCompleted()
    {
        if (Target == QuestTarget.POI) return;
        if (Target == QuestTarget.FlatArea) return;
        if (OwnerQuest.OwnerJournal == null) return;
        if (OwnerQuest.GetPositionData(out Vector3 pos, Quest.PositionDataTypes.POIPosition))
        {
            // Log.Out("## add trader poi to faction {0} => {1},{2}", OwnerQuest.QuestFaction, pos.x, pos.z);
            OwnerQuest.OwnerJournal.AddTraderPOI(new Vector2(pos.x, pos.z), OwnerQuest.QuestFaction);
            if (RepeatPOI == false)
            {
                string ID = "QuestFaction";
                byte faction = OwnerQuest.QuestFaction;
                if (OwnerQuest.DataVariables.ContainsKey(ID))
                    faction = byte.Parse(OwnerQuest.DataVariables[ID]);
                // Log.Out("## add trader poi to faction {0} => {1},{2}", faction, pos.x, pos.z);
                OwnerQuest.OwnerJournal.AddTraderPOI(new Vector2(pos.x, pos.z), faction);
            }
        }
    }

    //#####################################################
    // Implement for Custom Objectives, which is required
    // in order to support both MP and SP more easily.
    //#####################################################
    // Only the first objective should again take the initial position.
    // In order to implement this, I "abuse" the PositionData to add a
    // new entry there. If it does not exist, we are the first objective    
    // to get offered the starting position. All further objectives may
    // re-use that `PositionData` and use or even update it for next one.
    //#####################################################
    // We can't transport information for further objectives from server to
    // client, only the starting position is known (beside other meta info).
    // Therefore we MUST calculate further goto steps on the client side.
    // Once calculated it is stored inside PlayerData, with active quests
    //#####################################################


    protected QuestAnchor Anchor
        = QuestAnchor.QuestGiver;

    protected QuestTarget Target
        = QuestTarget.FlatArea;

    public bool IsTraderTarget =>
        Target == QuestTarget.Trader ||
        Target == QuestTarget.TraderNext ||
        Target == QuestTarget.TraderClosest;

    protected override string GetKeyword()
        => Localization.Get(KeywordProp);

    protected override void FetchAsyncStartPosition()
    {
        var pkg = NetPackageManager.GetPackage<NetPkgGetObjectivePosition>();

        Vector3 position = Vector3.zero;

#if DEBUG
        Log.Out("FetchAsyncStartPosition ({0}-{1})", MinDistance, MaxDistance);
        Log.Out("   Player {0}", Player?.position);
        Log.Out("   QuestGiver {0}", QuestGiver?.position);
        Log.Out("   QuestPosition {0}", OwnerQuest?.Position);
        Log.Out("   QuestStart {0}", OwnerQuest?.Position);
        Log.Out("   QuestCode {0}", OwnerQuest.QuestCode);
#endif

        if (Anchor == QuestAnchor.Player) position = Player.position;
        else if (Anchor == QuestAnchor.QuestGiver) position = QuestGiver?.position ?? Player.position;
        else if (Anchor == QuestAnchor.QuestPosition) position = OwnerQuest.Position;
        else if (Anchor == QuestAnchor.QuestStart) position = OwnerQuest.Position;
        else if (Target == QuestTarget.Reuse) Log.Error("Reuse must not call FetchAsync");

        byte faction = OwnerQuest.QuestFaction;
        if (QuestGiver is EntityNPC npc)
        {
            faction = npc.NPCInfo.QuestFaction;
        }

        pkg.Setup(OwnerQuest.QuestCode, position,
            MinDistance, MaxDistance, Target,
            faction, UpdateNPC && IsTraderTarget);

        SimulateServer.ProcessAtServer(pkg, Player);

        CurrentValue = 1;
    }

    protected override void ObjectiveActivated(bool loaded = false)
    {
        base.ObjectiveActivated(loaded);
    }

    //#####################################################
    // Implementation to allow cloning
    //#####################################################

    protected void CopyTo(ObjectiveRandomPlace into)
    {
        into.KeywordProp = KeywordProp;
        into.MinDistance = MinDistance;
        into.MaxDistance = MaxDistance;
        into.RepeatPOI = RepeatPOI;
        into.UpdateNPC = UpdateNPC;
        into.Anchor = Anchor;
        into.Target = Target;
        base.CopyTo(into);
    }

    public override BaseObjective Clone()
    {
        var clone = new ObjectiveRandomPlace();
        CopyTo(clone); // Invoke copy function
        return clone;
    }

    //#####################################################
    //#####################################################
    /*
        public override void Update(float deltaTime)
        {
            base.Update(deltaTime);
            OwnerQuest.PositionData.TryGetValue(Quest.
                PositionDataTypes.Location, out var Pos);
            Log.Out("Update {0}) {1}, {2}, {3}", CurrentValue,
                TargetPos, OwnerQuest.Position, Pos); 
        }
    */

    //#####################################################
    // Manual way of setting stuff via xml configs
    //#####################################################

    public override void ParseProperties(DynamicProperties properties)
    {
        base.ParseProperties(properties);
        if (properties.Values.ContainsKey("label"))
            KeywordProp = properties.Values["label"];
        if (properties.Values.ContainsKey("anchor"))
            Anchor = EnumUtils.Parse<QuestAnchor>(
                properties.Values["anchor"]);
        if (properties.Values.ContainsKey("target"))
            Target = EnumUtils.Parse<QuestTarget>(
                properties.Values["target"]);
        if (properties.Values.ContainsKey("min_distance"))
            MinDistance = properties.GetInt("min_distance");
        if (properties.Values.ContainsKey("max_distance"))
            MaxDistance = properties.GetInt("max_distance");
        if (properties.Values.ContainsKey("repeat_poi"))
            RepeatPOI = properties.GetBool("repeat_poi");
        if (properties.Values.ContainsKey("update_npc"))
            UpdateNPC = properties.GetBool("update_npc");
    }

    public override void ReadConfig(BinaryReader br)
    {
        base.ReadConfig(br);
        KeywordProp = br.ReadString();
        Anchor = (QuestAnchor)br.ReadByte();
        Target = (QuestTarget)br.ReadByte();
        MinDistance = br.ReadSingle();
        MaxDistance = br.ReadSingle();
        RepeatPOI = br.ReadBoolean();
        UpdateNPC = br.ReadBoolean();
    }
    
    public override void WriteConfig(BinaryWriter bw)
    {
        base.WriteConfig(bw);
        bw.Write(KeywordProp);
        bw.Write((byte)Anchor);
        bw.Write((byte)Target);
        bw.Write(MinDistance);
        bw.Write(MaxDistance);
        bw.Write(RepeatPOI);
        bw.Write(UpdateNPC);
    }

    //#####################################################
    //#####################################################
}
