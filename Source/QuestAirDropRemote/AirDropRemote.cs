using UnityEngine;

public class FlightPlan
{

    //#####################################################
    //#####################################################

    public static ObjectiveAirDrop Controller;

    // The visible plane entity
    public Entity SupplyPlane;

    // Main player owning the quest
    private readonly EntityPlayer Player;

    //#####################################################
    //#####################################################

    // This is our hook to check if our quest entities appeared
    public void EntitySpawnedInWorld(Entity entity)
    {
        if (entity.spawnById == Player.entityId)
        {
            if (entity is EntitySupplyPlane plane)
                Controller.SpawnedAirPlane(plane);
            else if (entity is EntityAirDrop crate)
                Controller.SpawnedAirDrop(crate);
        }
    }

    //#####################################################
    //#####################################################

    public FlightPlan(
      ObjectiveAirDrop controller,
      Vector3 position, EntityPlayer player)
    {
        Player = player;
        // Position = position;
        Controller = controller;

        var pkg = NetPackageManager.GetPackage<NetPkgSpawnAirDrop>()
            .Setup(player.entityId, controller.CrateKlass, position);

        // GameEventManager.Current.GameEntitySpawned += new OnGameEntityAdded(EntitySpawned);

        QuestsEvents.Instance.OnEntitySpawn += EntitySpawnedInWorld;

        // pkg.RegisterCrateWaiter(LocalCrateSpawned);
        // pkg.RegisterPlaneWaiter(LocalPlaneSpawned);

        SimulateServer.ProcessAtServer(pkg, player);

    }

    //#####################################################
    //#####################################################

}
