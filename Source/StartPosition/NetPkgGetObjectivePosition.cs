using System;
using System.Collections.Generic;
using UnityEngine;

public class NetPkgGetObjectivePosition : NetPackage
{
    private int QuestCode;
    private float MinDistance;
    private float MaxDistance;
    private Vector3 Position;
    private QuestTarget Target;
    private byte Faction;
    private bool UpdateNPC;
    private byte Tier;

    public override int GetLength()
    {
        return 42;
    }

    public NetPkgGetObjectivePosition Setup(
      int questCode,
      Vector3 position,
      float minDistance,
      float maxDistance,
      QuestTarget target,
      byte faction,
      bool updateNPC,
      byte tier = 255)
    {
        QuestCode = questCode;
        MinDistance = minDistance;
        MaxDistance = maxDistance;
        Position = position;
        Target = target;
        Faction = faction;
        UpdateNPC = updateNPC;
        Tier = tier;
        return this;
    }

    public bool IsPOI =>
        Target == QuestTarget.POI ||
        Target == QuestTarget.Trader ||
        Target == QuestTarget.TraderNext ||
        Target == QuestTarget.TraderClosest;

    public override void ProcessPackage(World _world, GameManager _callbacks)
    {

        EntityPlayer player = _world.GetEntity(Sender.entityId) as EntityPlayer;
        List<Tuple<byte, Vector3i>> updates = new List<Tuple<byte, Vector3i>>();
        var position = StartPosition.FindTargetPosition(ref updates, _world, player, Position,
            MinDistance, MaxDistance, Target, false, Faction, Tier);
        var pkg = NetPackageManager
            .GetPackage<NetPkgSetObjectivePosition>()
            .Setup(updates, QuestCode, position, UpdateNPC); // IsPOI
        SimulateServer.ProcessAtClient(pkg, player);
    }

    public override void read(PooledBinaryReader br)
    {
        QuestCode = br.ReadInt32();
        MinDistance = br.ReadSingle();
        MaxDistance = br.ReadSingle();
        Position = StreamUtils.ReadVector3(br);
        Target = (QuestTarget)br.ReadByte();
        Faction = br.ReadByte();
        UpdateNPC = br.ReadBoolean();
        Tier = br.ReadByte();
    }

    public override void write(PooledBinaryWriter bw)
    {
        base.write(bw);
        bw.Write(QuestCode);
        bw.Write(MinDistance);
        bw.Write(MaxDistance);
        StreamUtils.Write(bw, Position);
        bw.Write((byte)Target);
        bw.Write(Faction);
        bw.Write(UpdateNPC);
        bw.Write(Tier);
    }

}

