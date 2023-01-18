using HarmonyLib;

//###########################################################################
// A slightly imporoved `QuestEventManager`, mostly used to get
// informed about entity kills to drive our new horde spawner.
//###########################################################################

public class QuestsEvents : SingletonInstance<QuestsEvents>
{

    //########################################################
    // For Kill Quests we need to know if any entity died.
    // Unfortunately `QuestEventManager` only offers a very
    // dumbed down interface. E.g. we can't know which entity
    // exactly was killed. Fortunately this is only because
    // the registered functions have a too dumb signature.
    //########################################################

    // Create new event with better signature
    public delegate void QuestEvent_EntityKilled(
        Entity deceased, EntityAlive killer);

    public delegate void QuestEvent_EntitySpawned(Entity entity);

    public delegate void QuestEvent_EntityRemoved(
        Entity entity, EnumRemoveEntityReason reason);

    public event QuestEvent_EntityKilled OnEntityKill;
    public event QuestEvent_EntitySpawned OnEntitySpawn;
    public event QuestEvent_EntityRemoved OnEntityRemove;

    //########################################################
    //########################################################

    public void Cleanup()
    {
        OnEntityKill = null;
        OnEntitySpawn = null;
        OnEntityRemove = null;
        instance = null;
    }

    public void EntityKilled(Entity deceased, EntityAlive killer)
    {
        if (killer == null) return;
        if (deceased == null) return;
        if (OnEntityKill == null) return;
        OnEntityKill(deceased, killer);
    }

    public void EntitySpawned(Entity entity)
    {
        if (entity == null) return;
        if (OnEntitySpawn == null) return;
        OnEntitySpawn(entity);
    }

    public void EntityRemoved(Entity entity, EnumRemoveEntityReason reason)
    {
        if (entity == null) return;
        if (OnEntityRemove == null) return;
        OnEntityRemove(entity, reason);
    }

    //########################################################
    //########################################################

    [HarmonyPatch(typeof(QuestEventManager), "Cleanup")]
    private class QuestEventManagerCleanupPatch
    {
        static void Postfix()
        {
            // Only call cleanup if instance exists
            if (HasInstance) Instance.Cleanup();
        }
    }

    //########################################################
    //########################################################

    [HarmonyPatch(typeof(World), "SpawnEntityInWorld")]
    public class SpawnEntityInWorldPatch
    {
        static void Postfix(Entity _entity)
        {
            // This part patches for SP??
            if (_entity == null) return;
            if (instance == null) return;
            instance.EntitySpawned(_entity);
        }
    }


    [HarmonyPatch(typeof(World), "RemoveEntityFromMap")]
    public class RemoveEntityFromMapPatch
    {
        static void Prefix(Entity _entity, EnumRemoveEntityReason _reason)
        {
            // This part patches for SP??
            if (_entity == null) return;
            if (instance == null) return;
            instance.EntityRemoved(_entity, _reason);
        }
    }


    //########################################################
    //########################################################

    [HarmonyPatch(typeof(GameManager), "AwardKill")]
    private class GameManagerAwardKillPatch
    {
        static void Postfix(EntityAlive killer, Entity killedEntity)
        {
            // This part patches for SP??
            if (killer == null) return;
            if (killedEntity == null) return;
            if (instance == null) return;
            if (killer.isEntityRemote) return;
            instance.EntityKilled(killedEntity, killer);
        }
    }

    [HarmonyPatch(typeof(NetPackageEntityAwardKillServer), "ProcessPackage")]
    private class NetPackageEntityAwardKillServerProcessPackagePatch
    {
        static void Postfix(World _world, int ___EntityId, int ___KilledEntityId)
        {
            if (_world == null) return;
            if (instance == null) return;
            Entity killer = _world.GetEntity(___EntityId);
            if (!(killer is EntityPlayerLocal player)) return;
            Entity deceased = _world.GetEntity(___KilledEntityId);
            instance.EntityKilled(deceased, player);
        }
    }

    //########################################################
    //########################################################

}
