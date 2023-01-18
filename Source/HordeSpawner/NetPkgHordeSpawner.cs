using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;

//###########################################################################
//###########################################################################

public class NetPkgStartSpawner : NetPackage
{

    public HordeSpawnerCfg CFG;

    // HordeSpawners that we are ticking
    private static readonly List<HordeSpawner>
        Spawners = new List<HordeSpawner>();

    // public static readonly List<Action<Entity>>
    //     Listeners = new List<Action<Entity>>();

    public static void StopSpawner(int playerId)
    {
        Log.Out("Stop HordeSpawner for player {0}", playerId);
        foreach (var spawner in Spawners)
        {
            if (!spawner.BelongsToPlayer(playerId)) continue;
            spawner.Shutdown(false); // Signal shutdown to spawner
        }
        Spawners.RemoveAll(spawner =>
            spawner.BelongsToPlayer(playerId));
    }

    public NetPkgStartSpawner Setup(
        HordeSpawnerCfg cfg)
    {
        CFG = cfg;
        return this;
    }

    public override void read(PooledBinaryReader br)
    {
        CFG.ReadFrom(br);
    }

    public override void write(PooledBinaryWriter bw)
    {
        base.write(bw);
        CFG.WriteTo(bw);
    }

    // This is our hook to check if our quest entities appeared
    // public static void EntitySpawnedInWorld(Entity entity)
    // {
    //     foreach (var spawner in Spawners)
    //         spawner.EntitySpawnedInWorld(entity);
    //     foreach (var listener in Listeners)
    //         listener(entity);
    // }

    public override void ProcessPackage(World world, GameManager cbs)
    {
        CFG.PlayerID = Sender.entityId;
        if (CFG.TargetID == -1)
            CFG.TargetID = Sender.entityId;
        Spawners.Add(new HordeSpawner(CFG, world, Sender.entityId));
        if (Spawners.Count != 1) return;
        ModEvents.GameUpdate.RegisterHandler(TickSpawners);
    }

    private void TickSpawners()
    {
        for (int i = Spawners.Count - 1; i != -1; i -= 1)
        {
            HordeSpawner spawner = Spawners[i];
            if (!spawner.Tick(Time.deltaTime))
                Spawners.RemoveAt(i);
        }
        if (Spawners.Count == 0)
        {
            ModEvents.GameUpdate.UnregisterHandler(TickSpawners);
        }
    }


    public override NetPackageDirection PackageDirection => NetPackageDirection.ToServer;

    public override int GetLength() => 30;

    public GameRandom GetRandom()
    {
        return GameManager.Instance.World.GetGameRandom();
    }

    //########################################################
    //########################################################

    [HarmonyPatch(typeof(QuestEventManager), "Cleanup")]
    private class QuestEventManagerCleanupPatch
    {
        static void Postfix()
        {
            // Shutdown spawners and kill entities
            foreach (var spawner in Spawners)
                spawner.Shutdown(true);
            Spawners.Clear();
        }
    }

    //########################################################
    //########################################################

}
