using System.IO;
using UnityEngine;

//###########################################################################
// Entity class for a crate that can be locked by our quest objecivees.
//###########################################################################

public class EntityAirDrop : EntitySupplyCrate
{

    //#####################################################
    // Additional option to know if container is ready
    // to be looted (set once the quest is done).
    //#####################################################

    public bool IsReady = true;

    //#####################################################
    // Setup some settings for our entity
    // Base `EntitySupplyCrate` has these disabled
    //#####################################################

    // Can be pushed around by other entites
    public override bool CanBePushed() => true;

    // Can collide with any other entity class
    public override bool CanCollideWith(Entity _other) => true;

    // Make Air-Drop immune to bleeding and other damage buffs
    public override bool HasImmunity(BuffClass _buffClass) => true;

    //#####################################################
    // Persist state to recover from save files
    //#####################################################

    // Somehow the `belongsPlayerId` is not preserved
    // It is even reset if I restore it myself on read
    // So we read into another variable to update here
    int OwnerEntityId = -1;

    public override void Read(byte _version, BinaryReader _br)
    {
        base.Read(_version, _br);
        IsReady = _br.ReadBoolean();
        OwnerEntityId = _br.ReadInt32();
    }

    public override void Write(BinaryWriter _bw, bool _bNetworkWrite)
    {
        base.Write(_bw, _bNetworkWrite);
        _bw.Write(IsReady);
        // Restore player id from stored info (see above)
        if (belongsPlayerId == -1 && OwnerEntityId != -1)
            belongsPlayerId = OwnerEntityId;
        _bw.Write(belongsPlayerId);
    }

    //#####################################################
    // Make sure base doesn't remove our chunk observer
    // We want the crate loaded at all time for objective
    // Makes the objective more responsive for onGround
    //#####################################################

    public override void MoveEntityHeaded(Vector3 _direction, bool _isDirAbsolute)
    {
        var old = ChunkObserver;
        ChunkObserver = null;
        base.MoveEntityHeaded(_direction, _isDirAbsolute);
        ChunkObserver = old;
    }

    //#####################################################
    // Only allow interaction once container is ready
    //#####################################################

    public override EntityActivationCommand[] GetActivationCommands(
        Vector3i _tePos, EntityAlive _entityFocusing)
    {
        var cmds = base.GetActivationCommands(_tePos, _entityFocusing);
        for (int i = 0; i < cmds.Length; i += 1)
            cmds[i].enabled = IsReady;
        return cmds;
    }

    //#####################################################
    // Check if owner has gone away (e.g. logged out)
    // If so, we simply kill the crate and objective will
    // fail once the player loads back into the game.
    // Makes the whole multiplayer logic much easier.
    //#####################################################

    Entity Owner;

    public override void OnUpdateEntity()
    {
        base.OnUpdateEntity();
        // Only execute on server
        if (isEntityRemote) return;
        // Restore player id from stored info (see above)
        if (belongsPlayerId == -1 && OwnerEntityId != -1)
            belongsPlayerId = OwnerEntityId;
        // Try to fetch owner if possible
        if (belongsPlayerId != -1 && Owner == null)
            Owner = world.GetEntity(belongsPlayerId);
        // If there is no owner, kill the crate
        // if (Owner == null) Kill(DamageResponse.New(true));
    }

    //#####################################################
    // Avoid adding journal entry for supply crate
    //#####################################################

    protected override void Start() { }

    //#####################################################
    //#####################################################

}
