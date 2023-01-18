using System;
using System.Collections.Generic;
using UnityEngine;

//###########################################################################
// Main net package for clients to request an air-drop.
//###########################################################################

public class NetPkgSpawnAirDrop : NetPackage
{

    //#####################################################
    //#####################################################

    // ID of requesting player
    private int PlayerID;

    // Position where to drop it
    private Vector3 Position;

    // Implemented for controller interface
    // ClassID of the crate entity to spawn
    public int CrateKlass { get; set; } = -1;

    
    private World WRLD;

    //#####################################################
    //#####################################################

    // AirDrops that we are ticking
    static readonly List<AirDrop>
        AirDrops = new List<AirDrop>();

    static readonly Dictionary<int, Action<EntitySupplyPlane>>
        PlaneWaiters = new Dictionary<int, Action<EntitySupplyPlane>>();

    static readonly Dictionary<int, Action<EntityAirDrop>>
        CrateWaiters = new Dictionary<int, Action<EntityAirDrop>>();

    //#####################################################
    // This is our hook to check if our quest entities appeared
    //#####################################################

    public static void EntitySpawnedInWorld(Entity entity)
    {
        if (entity is EntitySupplyPlane plane)
        {
            // Check if the requested plane has appeared
            if (PlaneWaiters.TryGetValue(plane.spawnById,
                out Action<EntitySupplyPlane> cb))
            {
                PlaneWaiters.Remove(plane.spawnById);
                cb(plane);
            }
        }
        if (entity is EntityAirDrop crate)
        {
            // Check if the requested air drop crate appeared
            if (CrateWaiters.TryGetValue(crate.spawnById,
                out Action<EntityAirDrop> cb))
            {
                CrateWaiters.Remove(crate.spawnById);
                cb(crate);
            }
        }
    }

    public void RegisterPlaneWaiter(Action<EntitySupplyPlane> cb)
    {
        PlaneWaiters[PlayerID] = cb;
    }

    public void RegisterCrateWaiter(Action<EntityAirDrop> cb)
    {
        CrateWaiters[PlayerID] = cb;
    }

    public NetPkgSpawnAirDrop Setup(int playerId, int klass, Vector3 position)
    {
        PlayerID = playerId;
        Position = position;
        CrateKlass = klass;
        return this;
    }

    //#####################################################
    //#####################################################

    public override void read(PooledBinaryReader br)
    {
        PlayerID = br.ReadInt32();
        CrateKlass = br.ReadInt32();
        Position = StreamUtils.ReadVector3(br);
    }

    public override void write(PooledBinaryWriter bw)
    {
        base.write(bw);
        bw.Write(PlayerID);
        bw.Write(CrateKlass);
        StreamUtils.Write(bw, Position);
    }

    //#####################################################
    //#####################################################

    public override void ProcessPackage(World world, GameManager cbs)
    {
        if (world == null) return;

        WRLD = world;

        if (!(world.GetEntity(PlayerID) is EntityPlayer player)) return;

        AirDrops.Add(new AirDrop(world, Position, player, this));

        if (AirDrops.Count == 1)
        {
            ModEvents.GameUpdate.RegisterHandler(TickAirDrops);
        }
    }

    //#####################################################
    //#####################################################

    private void TickAirDrops()
    {
        for (int i = AirDrops.Count - 1; i != -1; i -= 1)
        {
            AirDrop drop = AirDrops[i];
            if (!drop.Tick(Time.deltaTime))
                AirDrops.RemoveAt(i);
        }
        if (AirDrops.Count == 0)
        {
            ModEvents.GameUpdate.UnregisterHandler(TickAirDrops);
        }
    }

    public override NetPackageDirection PackageDirection => NetPackageDirection.ToServer;

    public override int GetLength() => 30;

    public GameRandom GetRandom()
    {
        return GameManager.Instance.World.GetGameRandom();
    }

    public bool RequireFlightPlan()
    {
        return true;
    }

    public EntityAirDrop SpawnAirDrop(Vector3 pos)
    {
        // Create the air drop with explicit info for quest giver to know it's his
        EntityAirDrop drop = EntityFactory.CreateEntity(new EntityCreationData
        {
            entityClass = CrateKlass,
            id = EntityFactory.nextEntityID++,
            // blockValue = ItemValue.None.Clone(),
            itemStack = new ItemStack(ItemValue.None.Clone(), 1),
            pos = pos,
            rot = new Vector3(GetRandom().RandomFloat * 360f, 0.0f, 0.0f),
            lifetime = float.MaxValue,
            belongsPlayerId = PlayerID,
            spawnById = PlayerID,
            // spawnByName = "AirDrop"
        }) as EntityAirDrop;
        WRLD.SpawnEntityInWorld(drop);
        // Return crate entity
        return drop;
    }

}
