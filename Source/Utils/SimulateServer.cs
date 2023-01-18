public static class SimulateServer
{

    public static void ProcessAtServer(NetPackage pkg, Entity player)
    {

        if (ConnectionManager.Instance.IsClient)
        {
            ConnectionManager.Instance.SendToServer(pkg);
        }
        else
        {
            pkg.Sender = new ClientInfo { entityId = player.entityId };
            pkg.ProcessPackage(player.world, GameManager.Instance);
        }
    }

    public static void ProcessAtClient(NetPackage pkg, Entity player)
    {
        if (ConnectionManager.Instance.IsServer)
        {
            // Send to remote from dedicated server
            // Or to remote client when self-hosted
            if (GameManager.IsDedicatedServer || player.entityId !=
                GameManager.Instance.World?.GetPrimaryPlayerId())
            {
                ConnectionManager.Instance.SendPackage(pkg,
                    _attachedToEntityId: player.entityId);
                return; // All done, abort
            }
        }
        // Otherwise process locally
        pkg.Sender = new ClientInfo { entityId = player.entityId };
        pkg.ProcessPackage(player.world, GameManager.Instance);
    }

}

