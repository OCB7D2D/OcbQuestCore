//###########################################################################
//###########################################################################

public class NetPkgStopSpawner : NetPackage
{


    // public static readonly List<Action<Entity>>
    //     Listeners = new List<Action<Entity>>();

    int PlayerID = -1;

    public NetPkgStopSpawner Setup(
        int playerId)
    {
        PlayerID = playerId;
        return this;
    }

    public override void read(PooledBinaryReader br)
    {
        PlayerID = br.ReadInt32();
    }

    public override void write(PooledBinaryWriter bw)
    {
        base.write(bw);
        bw.Write(PlayerID);
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
        NetPkgStartSpawner.StopSpawner(PlayerID);
    }

    public override NetPackageDirection PackageDirection => NetPackageDirection.ToServer;

    public override int GetLength() => 30;

    //########################################################
    //########################################################

}
