using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//###########################################################################
// HordeSpawner works on the server side to spawn enemies according to
// the spawner config. Note that objectives run on the client and the
// kill count is not really synced from server to client.
//###########################################################################

public class HordeSpawner
{

    //#####################################################
    //#####################################################

    private readonly int PlayerID;
    private readonly World WRLD;
    HordeSpawnerCfg CFG;
    private int Kills;
    private float LastKill;

    private readonly List<int>
        Enemies = new List<int>();

    public HordeSpawner(HordeSpawnerCfg cfg,
        World world, int playerId)
    {
        CFG = cfg;
        WRLD = world;
        PlayerID = playerId;
    }

    public bool BelongsToPlayer(int playerId)
        => PlayerID == playerId;

    //#####################################################
    //#####################################################

    int RemainingSpawnCount => CFG.NeededKills + CFG.SpawnExcessCount - Kills;


    Coroutine Spawner;

    private GameStageDefinition GetRandomGameStage()
    {
        if (CFG.SpawnerGroups == null) return null;
        var rnd = Random.Range(0, CFG.SpawnerGroups.Count);
        string spawner = CFG.SpawnerGroups[rnd];
        return GameStageDefinition.GetGameStage(spawner);
    }

    static readonly HarmonyFieldProxy<int> FieldAttackTargetTime =
        new HarmonyFieldProxy<int>(typeof(EntityAlive), "attackTargetTime");

    static readonly HarmonyFieldProxy<float> FieldMoveSpeedScaleTime =
        new HarmonyFieldProxy<float>(typeof(EntityZombie), "moveSpeedScaleTime");

    static int GetAttackTime(EntityAlive entity)
    {
        return FieldAttackTargetTime.Get(entity) / 50;
    }

    private static void TargetEntity(Entity entity,
        GameRandom rnd, EntityAlive target,
        float retarget_chance = 0.2f,
        int min_target_time = 8,
        int max_target_time = 24)
    {
        if (!(entity is EntityAlive alive)) return;
        if (rnd.RandomDouble > retarget_chance) return;
        if (alive.GetAttackTarget() != null) return;
        if (alive.GetAttackTarget() == target) return;
        var time = rnd.RandomRange(min_target_time, max_target_time);
        alive.SetAttackTarget(target, time * 50);
    }

    public static void EnrageEntity(
        Entity entity, GameRandom rnd,
        float rage_chance = 0.25f,
        float rage_min_speed = 1.5f,
        float rage_max_speed = 2.5f,
        float rage_min_time = 12f,
        float rage_max_time = 24f)
    {
        if (!(entity is EntityZombie zombie)) return;
        if (rnd.RandomDouble > rage_chance) return;

        zombie.StartRage(
            rnd.RandomRange(rage_min_speed, rage_max_speed),
            rnd.RandomRange(rage_min_time, rage_max_time));
    }

    public static void SpawnQuestEntity(
        int spawnedEntityID,
        int entityIDQuestHolder,
        HordeSpawnerCfg cfg,
        EntityPlayer player = null,
        EntityAlive target = null,
        List<int> list = null,
        bool airborne = false)
    {
        if (player == null) return;
        if (target == null) return;
        if (target == null) target = player;
        World world = GameManager.Instance.World;
        if (world == null) return;
        var rnd = world.GetGameRandom();
        Vector3 _transformPos = player.position;
        float dist_sq = cfg.MinPlayerDistance * cfg.MinPlayerDistance;
        while ((_transformPos - player.position).sqrMagnitude < dist_sq)
        {
            if (player == null) player = world.GetEntity(entityIDQuestHolder) as EntityPlayer;
            Vector3 direction = new Vector3(world.GetGameRandom().RandomFloat * 2.0f - 1.0f,
                0f, world.GetGameRandom().RandomFloat * 2.0f - 1.0f); direction.Normalize();
            _transformPos = target.position + direction * rnd.RandomRange(cfg.MinPlayerDistance, cfg.MaxPlayerDistance);
            float height = GameManager.Instance.World.GetHeight((int)_transformPos.x, (int)_transformPos.z);
            float terrainHeight = GameManager.Instance.World.GetTerrainHeight((int)_transformPos.x, (int)_transformPos.z);
            _transformPos.y = (height + terrainHeight) / 2.0f;
        }

        Vector3 _rotation = new Vector3(0.0f, player.transform.eulerAngles.y + 180f, 0.0f);

        if (airborne) _transformPos.y = Mathf.Max(_transformPos.y,
                target.position.y + cfg.EntityHeightOffset);

        // Check what the right offset is to not fall through the world
        _transformPos.y += 0.5f;

        // Create the plane with explicit info for quest giver to know it's his
        Entity entity = EntityFactory.CreateEntity(new EntityCreationData
        {
            entityClass = spawnedEntityID,
            id = EntityFactory.nextEntityID++,
            // blockValue = ItemValue.None.Clone(),
            itemStack = new ItemStack(ItemValue.None.Clone(), 1),
            pos = _transformPos,
            rot = _rotation,
            lifetime = float.MaxValue,
            belongsPlayerId = player.entityId,
            spawnById = player.entityId,
            // spawnByName = player.EntityName
        });


        // Entity entity = EntityFactory.CreateEntity(spawnedEntityID, _transformPos, _rotation);

        entity.SetSpawnerSource(EnumSpawnerSource.Dynamic);
        GameManager.Instance.World.SpawnEntityInWorld(entity);

        if (rnd.RandomFloat < cfg.PlayerTargetChance) target = player;
        TargetEntity(entity, rnd, rnd.RandomFloat < cfg.PlayerTargetChance ? player
            : target, cfg.TargetChance, cfg.MinTargetTime, cfg.MaxTargetTime);
        EnrageEntity(entity, rnd, cfg.RageChance,
            cfg.MinRageSpeed, cfg.MaxRageSpeed,
            cfg.MinRageTime, cfg.MaxRageTime);
        Log.Out(" -> HordeSpawnEntity {0} at {1} (distance {2})",
            entity, entity.position, (entity.position - player.position).magnitude);
        if (list == null) return;
        if (!list.Contains(entity.entityId))
            list.Add(entity.entityId);
    }

    private void ClearDeadEnemies(WorldBase world)
    {
        // Clean up the entites list
        Enemies.RemoveAll(id =>
        {
            var entity = world.GetEntity(id);
            return entity == null || entity.IsDead();
        });
    }

    private IEnumerator SpawnEnemies(EntityPlayer player)
    {

        int LastClassId = -1;

        var RND = GameManager.Instance.World.GetGameRandom(); ;
        ModEvents.EntityKilled.RegisterHandler(EntityKilled);
        QuestsEvents.Instance.OnEntityRemove += EntityRemoved;

        // Set initial time for last kill
        // Used to spawn more if nothing goes
        LastKill = Time.time;

        float LastSpawn = LastKill;
        float LastReport = LastKill;
        float LastReRage = LastKill;
        float LastReTarget = LastKill;

        float waitForSpawn = Time.time + Random.Range(
            CFG.MinSpawnWait, CFG.MaxSpawnWait);

        if (!(WRLD.GetEntity(CFG.PlayerID) is EntityPlayer playerReUse) ||
            !(WRLD.GetEntity(CFG.TargetID) is EntityAlive target))
        {
            Log.Warning("Could not find player {0} and/or target {1}",
                WRLD.GetEntity(CFG.PlayerID),
                WRLD.GetEntity(CFG.TargetID));
            Spawner = null;
            yield break;
        }

        while (Kills < CFG.NeededKills && player.IsAlive())
        {

            if (player == null)
            {
                Log.Warning("Player went away, abort spawner");
                yield break;
            }

            // Clean up the entites list
            ClearDeadEnemies(WRLD);

            if (Enemies.Count > 0 && LastReTarget < Time.time)
            {
                var eid = Enemies[RND.RandomRange(0, Enemies.Count)];
                if (WRLD.GetEntity(CFG.TargetID) is EntityAlive entity)
                {
                    if (entity.GetAttackTarget() == null)
                    {
                        TargetEntity(entity, RND, RND.RandomFloat < CFG.PlayerReTargetChance ? player
                            : target, CFG.ReTargetChance, CFG.MinReTargetTime, CFG.MaxReTargetTime);
                    }
                }
                LastReTarget = Time.time + UnityEngine.Random.Range(
                        CFG.ReTargetMinDelay, CFG.ReTargetMaxDelay); ;
            }

            if (Enemies.Count > 0 && LastReRage < Time.time)
            {
                var eid = Enemies[RND.RandomRange(0, Enemies.Count)];
                if (WRLD.GetEntity(eid) is EntityZombie entity)
                {
                    if (FieldMoveSpeedScaleTime.Get(entity) <= 0f)
                    {
                        EnrageEntity(entity, RND, CFG.ReRageChance,
                            CFG.MinReRageSpeed, CFG.MaxReRageSpeed,
                            CFG.MinReRageTime, CFG.MaxReRageTime);
                    }
                }
                LastReRage = Time.time + UnityEngine.Random.Range(
                        CFG.ReRageMinDelay, CFG.ReRageMaxDelay);
            }

            if (LastReport + 4 < Time.time)
            {
                // Print out regular report about our horde spawner
                Log.Out("################################################");
                Log.Out("HordeSpawner has kills {0}/{1}, alive {2}, excess {3}",
                    Kills, CFG.NeededKills, Enemies.Count, CFG.SpawnExcessCount);
                Log.Out("################################################");
                foreach (var id in Enemies)
                {
                    if (WRLD.GetEntity(id) is EntityAlive entity)
                        Log.Out("  {0} targets {1} ({2}s)", entity,
                            entity.GetAttackTarget(),
                            GetAttackTime(entity));
                    // entity.CanSee(entity.GetAttackTarget() as EntityVulture)
                }
                Log.Out("################################################");
                LastReport = Time.time;
            }

            bool forceSpawn = (Time.time - Mathf.Max(LastSpawn, LastKill)) > 30f;

            if (forceSpawn || Enemies.Count < Mathf.Min(RemainingSpawnCount, CFG.MaxEntityCount))
            {

                if (forceSpawn || waitForSpawn < Time.time)
                {
                    // Get the random spawner group to choose entities
                    GameStageDefinition group = GetRandomGameStage();

                    // Get the stage and the correct spawner group
                    var stage = group.GetStage(player.PartyGameStage);
                    string name = stage.GetSpawnGroup(0).groupName;
                    bool birds = name.Contains("Vulture");
                    Log.Out("+++ HordeSpawner from group {0} (is bird {1}) (remaining {2})", name, birds, RemainingSpawnCount);
                    LastClassId = EntityGroups.GetRandomFromGroup(name, ref LastClassId, RND);
                    // Spawn the entity with the options from spawner
                    if (LastClassId != 0) SpawnQuestEntity(LastClassId, -1,
                        CFG, player, target, Enemies, birds);
                    // Make sure to wait between entites within burst

                    waitForSpawn = Time.time + Random.Range(
                        CFG.MinSpawnWait, CFG.MaxSpawnWait);
                    LastSpawn = Time.time;
                    forceSpawn = false;
                }

            }

            yield return new WaitForSeconds(0.25f);
        }

        ModEvents.EntityKilled.UnregisterHandler(EntityKilled);
        QuestsEvents.Instance.OnEntityRemove -= EntityRemoved;

        // Exit Spawner
        Spawner = null;

    }


    float UpdateTime;

    public bool Tick(float dt)
    {

        if (Spawner != null) return true;

        if (Kills >= CFG.NeededKills)
        {
            Log.Out("Abort spawn ticker, kills reached !!!!");
            return false;
        }
        
        if (UpdateTime > Time.time) return true;
        UpdateTime = Time.time + 1f;

        // Clean up the entites list
        ClearDeadEnemies(WRLD);

        var player = WRLD.GetEntity(CFG.PlayerID) as EntityPlayer;

        if (player == null || !player.IsAlive())
        {
            Shutdown(false);
            return false;
        }

        Log.Out("###############################################");
        Log.Out("HordeSpawner Kills {0}/{1}, Enemies {2} (max {3})",
            Kills, CFG.NeededKills, Enemies.Count, CFG.MaxEntityCount);
        Log.Out("###############################################");

        if (Enemies.Count < CFG.MaxEntityCount)
        {
            if (Enemies.Count < RemainingSpawnCount)
            {
                if (Spawner == null) Spawner = GameManager.Instance
                        .StartCoroutine(SpawnEnemies(player));
            }
        }

        return true;
    }

    //#####################################################
    //#####################################################

    private void EntityRemoved(Entity deceased, EnumRemoveEntityReason reason)
    {
        int idx = Enemies.IndexOf(deceased.entityId);
        if (idx == -1) return; // Check if valid target
        LastKill = Time.time; // Set time of last kill
        Enemies.RemoveAt(idx); // Remove entity from list
        Log.Out("- Despawned horde/server detected {0} [{4}] ({1}/{2}, left {3})",
            deceased.entityId, Kills, CFG.NeededKills, Enemies.Count, reason);
    }

    private void EntityKilled(Entity deceased, Entity killer)
    {
        if (killer?.entityId == CFG.PlayerID)
        {
            int idx = Enemies.IndexOf(deceased.entityId);
            if (idx == -1) return; // Check if valid target
            LastKill = Time.time; // Set time of last kill
            Kills += 1; // Increment kills toward goal
            Enemies.RemoveAt(idx); // Remove entity from list
            Log.Out("- Kill horde/server detected {0} ({1}/{2}, left {3})",
                deceased.entityId, Kills, CFG.NeededKills, Enemies.Count);
        }
    }

    public void EntitySpawnedInWorld(Entity entity)
    {
        if (entity?.spawnById == CFG.PlayerID)
        {
            Log.Out("+ Detected horde/server spawn {0} ({1}/{2}, left {3})",
                entity.entityId, Kills, CFG.NeededKills, Enemies.Count);
            if (!Enemies.Contains(entity.entityId))
                Enemies.Add(entity.entityId);
        }
    }

    //#####################################################
    //#####################################################

    public void Shutdown(bool killRest)
    {
        if (Spawner == null) return;
        GameManager.Instance.StopCoroutine(Spawner);
        foreach (int eid in Enemies)
        {
            if (WRLD.GetEntity(eid) is EntityAlive alive)
            {
                if (killRest)
                {
                    alive.DamageEntity(new DamageSource(
                        EnumDamageSource.Internal,
                        EnumDamageTypes.Suicide),
                        99999, false);
                }
                else
                {
                    // alive.SetAttackTarget(null, 0);
                }
            }
        }
        Enemies.Clear();
        Spawner = null;
    }

    //#####################################################
    //#####################################################

}
