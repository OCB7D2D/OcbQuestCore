
using System.IO;
using UnityEngine;

//###########################################################################
//###########################################################################

public class ObjectiveAirDrop : ObjectiveRandomPlace, IQuestTarget
{

    //#####################################################
    // Options configurable via xml properties
    //#####################################################

    // Start plane spawn when at target
    // Otherwise plane spawns right away
    bool StartAtPos = false;

    // Wait for completion until
    // crate is on the ground
    bool WaitForGround = true;

    // Complete once close to ground
    // ToDo: we currently cheat a bit
    int MinGroundHeight = -1;

    // The entity class to spawn
    string CrateClass = "AirSupplyCrate";

    // Nav object to track the plane flying
    string PlaneNavObject = "supply_plane";

    // Nav object once crate is out of plane
    string CrateNavObject = "fetch_container";

    // Lock crate at objective start
    bool LockAtStart = true;

    // Unlock crate once completed OK
    bool UnlockAtEnd = true;

    //#####################################################
    // Parse options from above from properties
    //#####################################################

    public override void ParseProperties(
        DynamicProperties properties)
    {
        base.ParseProperties(properties);
        if (properties.Values.ContainsKey("wait_ground"))
            WaitForGround = properties.GetBool("wait_ground");
        if (properties.Values.ContainsKey("start_at_pos"))
            StartAtPos = properties.GetBool("start_at_pos");
        if (properties.Values.ContainsKey("min_height"))
            MinGroundHeight = properties.GetInt("min_height");
        if (properties.Values.ContainsKey("crate_class"))
            CrateClass = properties.GetString("crate_class");
        if (properties.Values.ContainsKey("crate_nav_object"))
            CrateNavObject = properties.GetString("crate_nav_object");
        if (properties.Values.ContainsKey("plane_nav_object"))
            PlaneNavObject = properties.GetString("plane_nav_object");
        if (properties.Values.ContainsKey("lock_at_start"))
            LockAtStart = properties.GetBool("lock_at_start");
        if (properties.Values.ContainsKey("unlock_at_end"))
            UnlockAtEnd = properties.GetBool("unlock_at_end");
    }

    //#####################################################
    // State variable to run the whole objective
    //#####################################################

    // CrateEntity assigned to objective
    // Mostly needed to have a nice marker
    public EntityAirDrop CrateEntity;

    // ID to recover actual entity
    public int CrateEntityId = -1;

    // PlaneEntity assigned to objective
    // Only needed to have a nice marker
    public EntitySupplyPlane PlaneEntity;

    // ID to recover actual entity
    // Note: Planes are not persisted
    // int PlaneEntityId = -1;

    // Flag set once low enough
    // Or if crate is on the ground
    bool CrateIsReady = false;

    // Flag set once on the ground
    bool CrateOnGround = false;

    // Flag if player is in distance
    bool IsInDistance = false;

    // Flag if player was once in distance
    bool WasInDistance = false;

    // Flags for remote server support
    bool WaitForPlane = false;
    bool WaitForCrate = false;

    //#####################################################
    // Write necessary state only
    //#####################################################

    public override void Write(BinaryWriter _bw)
    {
        base.Write(_bw);
        if (CrateEntityId == -1 && CrateEntity != null)
            CrateEntityId = CrateEntity.entityId;
        _bw.Write(CrateEntityId);
        //_bw.Write(CrateIsReady);
        //_bw.Write(CrateOnGround);
        //_bw.Write(WasInDistance);
        //_bw.Write(WaitForPlane);
        //_bw.Write(WaitForCrate);
    }

    public override void Read(BinaryReader _br)
    {
        base.Read(_br);
        CrateEntityId = _br.ReadInt32();
        //CrateIsReady = _br.ReadBoolean();
        //CrateOnGround = _br.ReadBoolean();
        //WasInDistance = _br.ReadBoolean();
        //WaitForPlane = _br.ReadBoolean();
        //WaitForCrate = _br.ReadBoolean();
    }

    //#####################################################
    // Destroy the crate if the objective has failed
    //#####################################################

    public override void HandleFailed()
    {
        // Make sure we tried
        FetchCrateEntity();
        // Copied code from "killall"
        CrateEntity?.DamageEntity(
            new DamageSource(
                EnumDamageSource.Internal,
                EnumDamageTypes.Suicide),
                99999, false);
        // Further handle fail
        base.HandleFailed();
    }



    //#####################################################
    //#####################################################

    private bool IsOwnerQuestStateUnchangeable =>
        OwnerQuest?.CurrentState == Quest.QuestState.Completed ||
        OwnerQuest?.CurrentState == Quest.QuestState.ReadyForTurnIn;

    private void MarkObjectiveFailed()
    {
        // Remove positions as we abused them
        // Otherwise it tries to unlock POIs etc.
        OwnerQuest?.RemovePositionData(OcbQuestCore.QuestCratePosition);
        OwnerQuest?.RemovePositionData(OcbQuestCore.QuestPlanePosition);
        // Mark it as failed now
        OwnerQuest?.MarkFailed();
        // Really needed?
        Refresh();
    }

    //#####################################################
    // Helper to get crate entity from `id` when aborted
    //#####################################################

    public EntityAirDrop FetchCrateEntity()
    {
        if (GameManager.IsDedicatedServer) return null;
        // We already have the entity loaded, return it
        if (CrateEntity != null) return CrateEntity;
        // We have no crate entity id to fetch!?
        if (CrateEntityId == -1) return null;
        // Try to fetch the id from local world
        if (WRLD.GetEntity(CrateEntityId)
                is EntityAirDrop drop)
        {
            CrateEntity = drop;
        }
        // Return if CrateEntity C# is null or entity is dead
        if (CrateEntity?.IsDead() ?? true) return CrateEntity;
        // Check if complete to unlock if needed
        if (UnlockAtEnd && CurrentValue == LastValue)
            CrateEntity.IsReady |= true;
        // Inform that we found it now
        SpawnedAirDrop(CrateEntity);
        // Return fetched entity
        return CrateEntity;
    }

    //#####################################################
    //#####################################################

    public void UpdateCratePositionData()
    {
        if (CrateEntity == null) return;
        // Must set position data in order as we set the nav object to track it
        OwnerQuest?.SetPositionData(OcbQuestCore.QuestCratePosition, CrateEntity.position);
    }

    public void UpdatePlanePositionData()
    {
        if (PlaneEntity == null) return;
        // Must set position data in order as we set the nav object to track it
        OwnerQuest.SetPositionData(OcbQuestCore.QuestPlanePosition, PlaneEntity.position);
    }

    //#####################################################
    // Implementation for `IAirDropComponent`
    //#####################################################

    public bool RequireFlightPlan()
    {
        return FlightPlan == null && CrateEntityId == -1
            && (!StartAtPos || WasInDistance);
    }

    public void OnAirDropFound()
    {
        PlaneEntity = null; // un-track
        // Make sure it is locked at start
        CrateEntity.IsReady = !LockAtStart;
        // Update the crate/treasure point
        // Only used to track nav point
        UpdateCratePositionData();
        // Switch to track crate entity
        OwnerQuest.HandleMapObject(
            OcbQuestCore.QuestCratePosition,
            CrateNavObject);
        // Set nav object to track the entity
        OwnerQuest.NavObject.TrackedEntity = CrateEntity;
        // OwnerQuest.Tracked = true;
        // Update text label
        OnValueChanged();
        SetupDisplay();
    }

    public void SpawnedAirDrop(EntityAirDrop drop)
    {
        if (drop == null) return;
        CrateEntityId = drop.entityId;
        CrateEntity = drop;
        WaitForCrate = false;
        FlightPlan = null;
        OnAirDropFound();
    }

    public int CrateKlass => EntityClass.FromString(CrateClass);

    public GameRandom GetRandom() => RNG;

    //#####################################################
    //#####################################################

    // The current flight plan
    // Should produce a crate
    FlightPlan FlightPlan = null;

    private float LastReport = 0;

    protected override void ObjectiveActivated(bool loaded)
    {
        if (TargetPos == Vector3.zero)
        {
            OwnerQuest.PositionData.TryGetValue(
                Quest.PositionDataTypes.Location,
                out TargetPos);
        }

        if (loaded)
        {

            if (OwnerQuest.Position == Vector3.zero)
            {
                Log.Warning("Need to get Quest.Position from data (client only)");
                OwnerQuest.PositionData.TryGetValue(Quest.PositionDataTypes.Location,
                    out var target);
                {
                    TargetPos = OwnerQuest.Position = target;
                    Log.Out("   fetched traget position {0}", target);
                    // MarkTargetPosActive(target);
                }
            }
        }
        base.ObjectiveActivated();
    }

    protected void ExecuteDistanceCheck(float distance)
    {
        // To update if close to rally point
        IsInDistance = distance < CompletionDistance;
        WasInDistance |= IsInDistance; // was once there
    }

    // Main driver to check all states
    public override void Update(float dt)
    {

        // Can't do much without an owner
        // May happen is stuff is corrupt
        if (OwnerQuest == null) return;

        // Log.Out("Update AirDrop InDistance {0} {1} {2} -> {3}",
        //     IsInDistance, CrateEntityId, CrateEntity, CrateEntity?.ChunkObserver);

        // Wait for server to respond
        if (CurrentValue < 2)
        {
            base.Update(dt);
            // Log.Out("Tick update for air drop {0}", OwnerQuest.Position);
            // if (OwnerQuest.Position != Vector3.zero)
            //     MarkTargetPosActive(OwnerQuest.Position);
            return;
        }

        // Log.Out(" 1) {0} {1} {2}", IsInDistance, CrateEntityId, CrateEntity);

        // Check if whole quest was already completed
        // This happens if quest is set to `TurnIn`
        // Quest can not fail from that point on
        if (IsOwnerQuestStateUnchangeable)
        {
            // Remove our hooks
            RemoveHooks();
            CrateEntity = null;
            PlaneEntity = null;
            CrateEntityId = -1;
            // Nothing to see
            return;
        }

        if (LastReport < Time.time)
        {
            Log.Out("===================================================");
            Log.Out("AirDrop: crate-id {0} at {1}, state {2}, dist {3}/{4}",
                CrateEntityId, TargetPos, CurrentValue, IsInDistance, WasInDistance);
            Log.Out("  WaitForPlane {0}, WaitForCrate {1}, Plane: {2}, Crate, {3}",
                WaitForPlane, WaitForCrate, PlaneEntity?.entityId ?? -1, CrateEntity?.entityId ?? -1);
            // Update for next report time
            LastReport = Time.time + 5f;
        }

        if (CurrentValue < 2)
        {
            throw new System.Exception("Must have already a better state from activate");
        }

        if (CrateOnGround)
        {
            UpdateDistanceToEntity(Player, CrateEntity);
            ExecuteDistanceCheck(Distance);
        }
        else if (CrateEntity != null)
        {
            UpdateDistanceFromPlayer(Player);
            ExecuteDistanceCheck(Distance);
            UpdateDistanceToEntity(Player, CrateEntity);
            UpdateCratePositionData();
        }
        else if (PlaneEntity != null)
        {
            UpdateDistanceFromPlayer(Player);
            ExecuteDistanceCheck(Distance);
            UpdateDistanceToEntity(Player, PlaneEntity);
            UpdatePlanePositionData();
        }
        else
        {
            UpdateDistanceFromPlayer(Player);
            ExecuteDistanceCheck(Distance);
        }

        if (Time.time <= LastUpdateTime) return;
        LastUpdateTime = Time.time + TickInterval;

        SetupDisplay();

        // Check if crate was unloaded
        if (CrateEntityId != -1 && CrateEntity == null)
        {
            Entity entity = WRLD.GetEntity(CrateEntityId);
            var drop = entity as EntityAirDrop;
        }

        // Get entity (if needed) and calls `OnAirDropFound`
        // when the entity is first found (sets `CrateEntity`)
        FetchCrateEntity(); // We are the only user, inline?

        // Check if crate was spawned but got lost
        // In this case the whole quest is aborted
        // https://blog.unity.com/technology/custom-operator-should-we-keep-it
        if (CrateEntityId != -1 && (CrateEntity?.IsDead() ?? true))
        {
            // Play safe and abort it
            MarkObjectiveFailed();
            // Reset main objects
            CrateEntity = null;
            PlaneEntity = null;
            CrateEntityId = -1;
            // Abort objective
            return;
        }

        // Check if we need a plane to drop a crate
        // Starts only if no crate is there yet and
        // we have no other flight plan and the player
        // has reached the target area or if the
        // `start_at_pos` option was set to false
        if (RequireFlightPlan())
        {
            // Create a flight plan (may run remotely on server)
            FlightPlan = new FlightPlan(this, TargetPos, Player);
            // We are now waiting for stuff (if we have a plan)
            // ToDo: this should always be true, but play safe
            WaitForPlane = WaitForCrate = (FlightPlan != null);
            OnValueChanged();
            SetupDisplay();
        }


        // Check if create has reached the ground
        // It only needs to get to ground state once
        if (CrateOnGround == false
            && CrateEntity != null
            && CrateEntity.onGround)
        {
            CrateOnGround = true;
            OnValueChanged();
            SetupDisplay();
        }

        // Check if crate state allow to complete the objective
        // That may be when the crate is still mid air (vultures)
        // Or maybe when on ground and player is close by to it
        if (IsCreateReadyToComplete())
        {
            OwnerQuest?.RemovePositionData(OcbQuestCore.QuestCratePosition);
            OwnerQuest?.RemovePositionData(OcbQuestCore.QuestPlanePosition);
            // Optional unlock if it is locked
            CrateEntity.IsReady |= UnlockAtEnd;
            // Update text label
            OnValueChanged();
            SetupDisplay();
            // Progress state to end
            CurrentValue = LastValue;
            // We are complete
            HandleRemoveHooks();
            // Will complete
            Refresh();
        }
    }

    private bool IsCreateReadyToComplete()
    {
        if (WaitForGround)
        {
            return CrateOnGround && IsInDistance;
        }
        else if (MinGroundHeight != -1)
        {
            if (CrateEntity != null && OwnerQuest != null)
            {
                var player = OwnerQuest.OwnerJournal.OwnerPlayer.PlayerUI.entityPlayer;
                return Mathf.Abs(CrateEntity.position.y - player.position.y) < MinGroundHeight;
            }
        }
        return CrateEntity != null;
    }

    //#####################################################
    //#####################################################

    public void CopyTo(ObjectiveAirDrop into)
    {
        base.CopyTo(into);
        into.CrateClass = CrateClass;
        into.StartAtPos = StartAtPos;
        into.LockAtStart = LockAtStart;
        into.UnlockAtEnd = UnlockAtEnd;
        into.WaitForGround = WaitForGround;
        into.MinGroundHeight = MinGroundHeight;
        into.CrateNavObject = CrateNavObject;
        into.PlaneNavObject = PlaneNavObject;
        into.CrateIsReady = CrateIsReady;
        into.CrateOnGround = CrateOnGround;
        into.CrateEntityId = CrateEntityId;
        into.CrateEntity = CrateEntity;
        into.FlightPlan = FlightPlan;
        into.IsInDistance = IsInDistance;
        into.WasInDistance = WasInDistance;
        into.WaitForCrate = WaitForCrate;
        into.WaitForPlane = WaitForPlane;
    }

    //#####################################################
    // Implementation to allow cloning
    //#####################################################

    public override BaseObjective Clone()
    {
        ObjectiveAirDrop clone = new ObjectiveAirDrop();
        CopyTo(clone);
        return clone;
    }

    //#####################################################
    //#####################################################

    // Text is shown on the heads-up quest info
    protected override void OnValueChanged()
    {
        if (CurrentValue == 0) keyword = Localization.Get("ObjectiveWaitForStart");
        else if (CurrentValue == 1) keyword = Localization.Get("ObjectiveWaitForServer");
        else if (CrateOnGround) keyword = Localization.Get("ObjectiveGetToAirDrop");
        else if (CrateEntityId != -1) keyword = Localization.Get("ObjectiveWaitForTouchDown");
        else if (WaitForPlane) keyword = Localization.Get("ObjectiveWaitForFlightPlan");
        else if (WaitForCrate) keyword = Localization.Get("ObjectiveWaitForAirDrop");
        else if (StartAtPos) keyword = Localization.Get("ObjectiveRallyPointHeadTo");
        else keyword = Localization.Get("ObjectiveWaitForFlightPlan");
    }

    // Called for all objectives when quest starts
    public override void SetupObjective()
    {
        base.SetupObjective();
        OnValueChanged();
    }

    //#####################################################
    // Async hooks called via the (Remote) AirDrop
    //#####################################################

    public void SpawnedAirPlane(EntitySupplyPlane plane)
    {
        if (plane == null) return;
        if (WaitForPlane == false) return;
        // Register plane
        WaitForPlane = false;
        PlaneEntity = plane;
        // Update the plane/poi point
        // Only used to track nav point
        // Log.Out("+ Stuup Plane POI");
        // Fails instantly once crate drops?
        UpdatePlanePositionData();
        // Switch to track plane entity
        OwnerQuest.HandleMapObject(
            OcbQuestCore.QuestPlanePosition,
            PlaneNavObject);
        // Set nav object to track the entity
        OwnerQuest.NavObject.TrackedEntity = PlaneEntity;
        // OwnerQuest.Tracked = true;
        // Update text label
        OnValueChanged();
        SetupDisplay();
    }

    //#####################################################
    // Implementation for HordeSpawner to target crate
    //#####################################################

    public int GetTargetId()
    {
        // Return ID of crate if known
        if (CrateEntityId != -1)
            return CrateEntityId;
        // Otherwise target the player
        return OwnerQuest.OwnerJournal
              .OwnerPlayer.entityId;
    }

    //#####################################################
    //#####################################################

}
