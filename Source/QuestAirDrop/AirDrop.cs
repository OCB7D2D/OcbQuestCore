using UnityEngine;

//###########################################################################
//###########################################################################

public class AirDrop
{

    public static NetPkgSpawnAirDrop Spawner;

    private readonly World World;

    // The calculated flight path to follow
    private FlightPath FlightPlan;

    // Set to true once chunk is loaded
    // Before we must not spawn anything
    private bool SpawnCrate;

    // The visible plane entity
    public Entity SupplyPlane;

    // Main player owning the quest
    private readonly EntityPlayer Player;

    private Vector3 Position;

    protected float Delay = 10;

    public AirDrop(
      World world,
      Vector3 position,
      EntityPlayer player,
      NetPkgSpawnAirDrop spawner)
    {
        World = world;
        Position = position;
        Player = player;
        Spawner = spawner;
    }

    //#####################################################
    // Tick for AirDrop always runs on the server.
    // Waits for the delay until it spawns the AirPlane.
    // Then waits to spawn the crate on calculated time.
    //#####################################################

    public bool Tick(float dt)
    {

        // Spawner runs on the server, therefore the player may
        // disconnect at any time. In case this happens, abort
        // the flight plan to not spawn any crate. Once the crate
        // is out, it is out of our juristiction to kill it.
        if (Player == null || !Player.IsAlive())
        {
            Log.Warning("AirDrop: Player lost, abort flight plan");
            FlightPlan = null;
            return false;
        }

        // Is initial init needed?
        if (FlightPlan == null)
        {
            FlightPlan = CreateFlightPaths(Player, Delay);
            Log.Out("AirDrop: Computed flight paths for 1 aircraft.");
            Log.Out("AirDrop: Waiting for supply crate chunk locations to load...");
        }

        // Check if chunk is loaded to spawn crates
        if (!SpawnCrate)
        {
            Vector3i pos = World.worldToBlockPos(FlightPlan.Crate.SpawnPos);
            SpawnCrate = World.GetChunkFromWorldPos(pos) != null;
        }

        // Spawn crate soon
        if (SpawnCrate)
        {
            FlightPlan.Delay -= dt;
            if (FlightPlan.Delay <= 0.0)
            {
                if (!FlightPlan.Spawned)
                {
                    SpawnPlane(FlightPlan.Start, FlightPlan.End);
                    FlightPlan.Spawned = true;
                }

                FlightPlan.Crate.Delay -= dt;
                if (FlightPlan.Crate.Delay <= 0.0)
                {
                    EntityAirDrop entitySupplyCrate = Spawner
                        .SpawnAirDrop(FlightPlan.Crate.SpawnPos);
                    if (entitySupplyCrate != null) entitySupplyCrate
                        .ChunkObserver = FlightPlan.Crate.ChunkRef;
                    else if (FlightPlan.Crate.ChunkRef != null)
                    {
                        World.GetGameManager().RemoveChunkObserver(FlightPlan.Crate.ChunkRef);
                        FlightPlan.Crate.ChunkRef = null;
                    }
                    Log.Out("AirDrop: Spawned supply crate at " + FlightPlan.Crate.SpawnPos.ToCultureInvariantString()
                        + ", plane is at " + (SupplyPlane != null ? SupplyPlane.position : Vector3.zero).ToString());
                    // Spawner.SpawnedAirDrop(entitySupplyCrate);
                    FlightPlan.Crate = null;
                    FlightPlan = null;
                }
            }
        }
        
        // Return false once done
        return FlightPlan != null;
    }

    //#####################################################
    //#####################################################

    public static float Angle(Vector2 p_vector2) => p_vector2.x < 0.0f ?
        (360.0f - Mathf.Atan2(p_vector2.x, p_vector2.y) * 57.295780181884766f * -1.0f)
        : Mathf.Atan2(p_vector2.x, p_vector2.y) * 57.29578f;

    //#####################################################
    //#####################################################

    private void SpawnPlane(Vector3 start, Vector3 end)
    {
        // Calculate flight parameters
        Vector3 delta = end - start;
        Vector3 normalized = delta.normalized;
        Vector2 direction = new Vector2(normalized.x, normalized.z);
        // Create the plane with explicit info for quest giver to know it's his
        EntitySupplyPlane plane = EntityFactory.CreateEntity(new EntityCreationData
        {
            pos = start, id = EntityFactory.nextEntityID++,
            entityClass = EntityClass.FromString("AirSupplyPlane"),
            itemStack = new ItemStack(ItemValue.None.Clone(), 1),
            rot = new Vector3(0.0f, Angle(direction), 0.0f),
            lifetime = float.MaxValue,
            belongsPlayerId = Player.entityId,
            spawnById = Player.entityId,
            // spawnByName = Player.EntityName
        }) as EntitySupplyPlane;

        // Set the plane direction to fly to (check: add rigidbody motion?)
        plane.SetDirectionToFly(normalized, (int)(20f * (delta.magnitude / 120f + 10f)));

        World.SpawnEntityInWorld(plane);
        Log.Out("AirDrop: Spawned Air Plane at (" + start.ToCultureInvariantString()
            + "), heading (" + direction.ToCultureInvariantString() + ")");
        // Spawner.SpawnedAirPlane(plane);
        SupplyPlane = plane;
    }

    //#####################################################
    //#####################################################

    private FlightPath CreateFlightPaths(EntityPlayer player, float Delay = 10f)
    {

        GameRandom random = Spawner.GetRandom();

        AirDropSpawn crate = new AirDropSpawn();

        float height = World.GetHeightAt((int)Position.x, (int)Position.z);
        height = Utils.FastMin(random.RandomRange(height + 160, height + 190), 276f);
        var drop_pos = new Vector3(Position.x, height - 4f, Position.z);
        crate.SpawnPos = World.ClampToValidWorldPos(drop_pos);

        FlightPath flightPath = new FlightPath();

        Vector2 drop_point = new Vector2(drop_pos.x, drop_pos.z);
        Vector2 start_point = new Vector2(player.position.x, player.position.z);
        Vector2 direction = (drop_point - start_point).normalized;
        Vector2 end_point = drop_point + direction * 1500;
        start_point = drop_point - direction * 1500;
        flightPath.Start = new Vector3(start_point.x, height, start_point.y);
        flightPath.End = new Vector3(end_point.x, height, end_point.y);

        crate.Delay = (flightPath.End - flightPath.Start).magnitude / 240f;
        crate.ChunkRef = World.GetGameManager().AddChunkObserver(crate.SpawnPos, false, 3, -1);

        flightPath.Crate = crate;

        flightPath.Delay = Delay + random.RandomRange(0f, 2f);

        Log.Out("Created FlighPath {0} {1} {2}", flightPath.Delay, flightPath.Start, flightPath.End);
        Log.Out(" Will spawn Crate at {0} after {1}s", flightPath.Crate.SpawnPos, flightPath.Crate.Delay);

        return flightPath;

    }

    //#####################################################
    // Structs holding the flight plan data
    // Basically copied from vanilla code
    //#####################################################

    private class AirDropSpawn
    {
        public float Delay;
        public Vector3 SpawnPos;
        public ChunkManager.ChunkObserver ChunkRef;
    }

    private class FlightPath
    {
        public AirDropSpawn Crate;
        public Vector3 Start;
        public Vector3 End;
        public float Delay;
        public bool Spawned;
    }

    //#####################################################
    //#####################################################

}
