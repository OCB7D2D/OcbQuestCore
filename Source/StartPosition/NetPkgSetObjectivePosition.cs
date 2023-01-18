using System;
using System.Collections.Generic;
using UnityEngine;

//###########################################################################
//###########################################################################

public class NetPkgSetObjectivePosition : NetPackage
{

    private int QuestCode;
    private Vector3 Position;
    private bool UpdateNPC;
    List<Tuple<byte, Vector3i>> Updates;

    public override int GetLength()
    {
        return 42;
    }

    public NetPkgSetObjectivePosition Setup(
        List<Tuple<byte, Vector3i>> updates,
      int questCode,
      Vector3 position,
      bool updateNPC)
    {
        QuestCode = questCode;
        Position = position;
        Updates = updates;
        UpdateNPC = updateNPC;
        return this;
    }

    // Vanilla quest finding is shacky at best (and outwright incapable of not failing)
    // Best I can think of to fix this is to just select the next quest that need a position
    // This will allow at least to have multiple quests getting a proper starting position
    // Note: this isn't really deterministic, but there is no unique identifier for quests!?
    public static Quest FindActiveQuestWihtoutPosition(QuestJournal journal, int questCode)
    {
        for (int i = 0; i < journal.quests.Count; i++)
        {
            if (journal.quests[i].QuestCode == questCode && (journal.quests[i].CurrentState == Quest.QuestState.InProgress || journal.quests[i].CurrentState == Quest.QuestState.ReadyForTurnIn))
            {
                // This is really a bad heuristic, but the best I can think of to make it work :-/
                if (!journal.quests[i].PositionData.ContainsKey(OcbQuestCore.QuestNewPosition))
                {
                    return journal.quests[i];
                }
            }
        }

        return null;
    }


    // Processed at the client, since set is sent from the server
    public override void ProcessPackage(World _world, GameManager _callbacks)
    {
        if (!ConnectionManager.Instance.IsClient)
            Log.Error("SetObjectivePosition only allowed on client");
        EntityPlayer player = _world.GetPrimaryPlayer(); // _world.GetEntity(Sender.entityId) as EntityPlayer;
        if (FindActiveQuestWihtoutPosition(player.QuestJournal, QuestCode) is Quest quest)
        {
            quest.SetPositionData(OcbQuestCore.QuestNewPosition, Position);
            if (UpdateNPC) quest.SetPositionData(Quest.PositionDataTypes.QuestGiver, Position);
            // if (IsPOI) quest.SetPositionData(Quest.PositionDataTypes.POIPosition, position);
            foreach (var item in Updates)
            {
                quest.SetPositionData((Quest.PositionDataTypes)item.Item1, item.Item2);
            }
            // GameManager.Instance.StartCoroutine(SetPositionDelayed(quest, Position));
        }
    }

    public override void read(PooledBinaryReader br)
    {
        QuestCode = br.ReadInt32();
        Position = StreamUtils.ReadVector3(br);
        Updates = new List<Tuple<byte, Vector3i>>();
        for (int i = 0, size = br.ReadInt32(); i < size; ++i)
        {
            Updates.Add(new Tuple<byte, Vector3i>(br.ReadByte(),
                StreamUtils.ReadVector3i(br)));
        }
    }

    public override void write(PooledBinaryWriter bw)
    {
        base.write(bw);
        bw.Write(QuestCode);
        StreamUtils.Write(bw, Position);
        bw.Write(Updates.Count);
        foreach (var update in Updates)
        {
            bw.Write(update.Item1);
            StreamUtils.Write(bw, update.Item2);
        }
    }


}

