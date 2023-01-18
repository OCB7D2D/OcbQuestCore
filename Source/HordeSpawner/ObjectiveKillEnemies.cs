//###########################################################################
//###########################################################################

using System;
using System.Collections.Generic;
using System.IO;

public class ObjectiveKillEnemies : BaseCustomObjective
{

    //#####################################################
    // Spawner settings
    //#####################################################

    HordeSpawnerCfg SpawnerCfg
        = new HordeSpawnerCfg();

    //#####################################################
    //#####################################################

    // List of entities to kill
    private readonly List<int>
        Enemies = new List<int>();

    //#####################################################
    //#####################################################

    // Inform base that we use the update hooks
    protected override bool useUpdateLoop => true;

    // Inform base that we need to fulfill a number requirement
    public override ObjectiveValueTypes ObjectiveValueType => ObjectiveValueTypes.Number;

    //#####################################################
    // Manual way of setting stuff via xml configs
    //#####################################################

    public override void ParseProperties(DynamicProperties properties)
    {
        // Parsing after attributes
        base.ParseProperties(properties);
        // Dispatch to config struct for parsing
        SpawnerCfg.ParseProperties(properties);
        // Ensure NeededKills is also taken from attribute
        SpawnerCfg.NeededKills = Convert.ToInt32(Value);
        // Get list of spawner groups
        var groups = ID?.Split(',');
        for (int i = 0; i < groups.Length; i++)
            SpawnerCfg.AddSpawnGroup(groups[i]);
    }

    //#####################################################
    // Implementation to allow cloning
    //#####################################################

    public void CopyTo(ObjectiveKillEnemies into)
    {
        base.CopyTo(into);
        // Copy all config settings
        into.SpawnerCfg.CopyFrom(SpawnerCfg);
    }

    public override BaseObjective Clone()
    {
        var clone = new ObjectiveKillEnemies();
        CopyTo(clone);
        return clone;
    }

    //#####################################################
    //#####################################################

    public override void Write(BinaryWriter _bw)
    {
        base.Write(_bw);
        if (Enemies == null)
        {
            _bw.Write(-1);
        }
        else
        {
            _bw.Write(Enemies.Count);
            foreach (int id in Enemies)
                _bw.Write(id);
        }
    }

    public override void Read(BinaryReader _br)
    {
        base.Read(_br);
        int count = _br.ReadInt32();
        if (count <= 0) return;
        for (int i = 0; i < count; i++)
            Enemies.Add(_br.ReadInt32());
    }


    //#####################################################
    //#####################################################

    public override void AddHooks()
    {

        QuestsEvents.Instance.OnEntitySpawn += SpawnedEnemy;
        QuestsEvents.Instance.OnEntityKill += OnZombieKill;

        var pkg = NetPackageManager
            .GetPackage<NetPkgStartSpawner>()
            .Setup(SpawnerCfg);

        pkg.CFG.PlayerID = Player.entityId;

        // Set default target to player
        pkg.CFG.TargetID = Player.entityId;

        // Update config for optional objective target
        // Go backwards from current phase to find last one
        var idx = OwnerQuest.GetObjectiveIndex(this);
        for (int i = idx; i != -1; i -= 1)
        {
            var objective = OwnerQuest.Objectives[i];
            if (objective is IQuestTarget target)
            {
                int id = target.GetTargetId();
                if (id == -1) continue;
                pkg.CFG.TargetID = id;
                break;
            }
        }

        // Reduce needed kills from current state
        // Happens when quest objective is reloaded
        pkg.CFG.NeededKills -= CurrentValue;

        // Execute the spawner at the server
        SimulateServer.ProcessAtServer(pkg, Player);

        base.AddHooks();
    }


    public override void RemoveHooks()
    {
        QuestsEvents.Instance.OnEntitySpawn -= SpawnedEnemy;
        QuestsEvents.Instance.OnEntityKill -= OnZombieKill;
        base.RemoveHooks();
    }

    //#####################################################
    //#####################################################

    public override void SetupDisplay()
    {
        Description = keyword = Localization.Get("ObjectiveKillEnemies");
        StatusText = string.Format("{0}/{1}", CurrentValue, SpawnerCfg.NeededKills);
    }

    //#####################################################
    //#####################################################

    // TODo: Check if required!? (yes it is)
    public override void Refresh()
    {
        if (Complete) return;
        Complete = CurrentValue >= SpawnerCfg.NeededKills;
        if (!Complete) return;
        OwnerQuest.CheckForCompletion();
    }

    public override void Update(float deltaTime)
    {
        // Little backup and allows to set NeededKills to zero
        if (!Complete && CurrentValue >= SpawnerCfg.NeededKills)
        {
            Complete = true;
            OwnerQuest.CheckForCompletion();
        }
        base.Update(deltaTime);
    }

    //#####################################################
    //#####################################################


    private void OnZombieKill(
        Entity deceased, EntityAlive killer)
    {

        if (Complete) return;
        if (Enemies.Count == 0) return;

        for (int i = 0; i < Enemies.Count; i++)
        {
            int eid = Enemies[i];
            if (eid != deceased.entityId) continue;
            Enemies.Remove(eid); // Remove from targets
            if (killer.entityId != Player.entityId) break;
            CurrentValue += 1;
            Refresh();
            break;
        }
    }

    private void SpawnedEnemy(Entity enemy)
    {
        if (enemy.spawnById != Player.entityId) return;
        Enemies.Add(enemy.entityId);
    }

    //#####################################################
    //#####################################################

    public override void HandleFailed()
    {
        var pkg = NetPackageManager
            .GetPackage<NetPkgStopSpawner>()
            .Setup(Player.entityId);
        SimulateServer.ProcessAtServer(pkg, Player);
        base.HandleFailed();
    }

    // public override void HandlePhaseCompleted()
    // {
    //     base.HandlePhaseCompleted();
    // }
    // 
    // public override void HandleCompleted()
    // {
    //     base.HandleCompleted();
    // }

}
