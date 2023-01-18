using System;
using System.Collections.Generic;
using UnityEngine;

//###########################################################################
// Static helper class to find start positions for quests
// ToDo: this is currently in WIP/POC state (use at own risk)
//###########################################################################

public enum QuestAnchor
{
    QuestPosition,
    QuestGiver,
    QuestStart,
    TraderPosition,
    Player,
}

public enum QuestTarget
{
    POI,
    Reuse,
    FlatArea,
    Trader,
    TraderNext,
    TraderClosest,
}

//###########################################################################
//###########################################################################

public static class StartPosition
{

    //#####################################################
    // Tries to find a valid location, or if nothing found
    // after some trials, fallback to any random location.
    //#####################################################

    static public Vector3 FindTargetPosition(
        ref List<Tuple<byte, Vector3i>> updates,
        World world, EntityPlayer player, Vector3 position,
        float minDistance, float maxDistance, QuestTarget target,
        bool force = true, byte faction = 0,
        /*List<Vector2> excludes = null,*/ byte tier = 255)
    {

        // if (Target == QuestTarget.Trader)
        //     int factionID = trader != null ? trader.NPCInfo.QuestFaction : OwnerQuest.QuestFaction;

        // var exclude = Player.QuestJournal.GetTraderList(factionID);


        if (!ConnectionManager.Instance.IsServer)
            Log.Error("FindTargetPosition only allowed on server");

        // Log.Out("++ Find {2} for faction {0}, tier {1} around {3}", faction, tier, target, position);

        if (target == QuestTarget.Trader)
        {
            return FindTrader(ref updates, world, position, ignoreCurrentPOI: true);
        }
        else if (target == QuestTarget.TraderNext)
        {
            List<Vector2> exclude = player.QuestJournal.GetTraderList(faction);
            // foreach(var qwe in player.QuestJournal.TradersByFaction)
            // {
            //     Log.Out("  Trader Faction {0} => {1}", qwe.Key, qwe.Value.Count);
            // }
            // if (exclude == null) Log.Out("There is no exclude list for trader faction {0}", faction);
            // else Log.Out("==> Try to get next trader {0} => {1}", faction, string.Join(", ", exclude));
            return FindTrader(ref updates, world, position, exclude: exclude);
        }
        else if (target == QuestTarget.TraderClosest)
        {
            return FindTrader(ref updates, world, position, ignoreCurrentPOI: false);
        }
        else if (target == QuestTarget.POI)
        {

            tier = (byte)255; // : OwnerQuest.QuestClass.DifficultyTier;
            // Log.Out("======= GET RANDOM POI NEAR {0} ({1}-{2})", position, minDistance, maxDistance);
            var prefab = GetRandomPOINearWorldPos(
                new Vector2(position.x, position.z),
                (int)(minDistance * minDistance),
                (int)(maxDistance * maxDistance),
                FastTags.none, tier, null);

            if (prefab == null) return Vector3.zero;

            updates.Add(new Tuple<byte, Vector3i>((byte)Quest.PositionDataTypes.POIPosition, prefab.boundingBoxPosition));
            updates.Add(new Tuple<byte, Vector3i>((byte)Quest.PositionDataTypes.POISize, prefab.boundingBoxSize));

            int x = (int)(prefab.boundingBoxPosition.x + prefab.boundingBoxSize.x / 2f);
            int z = (int)(prefab.boundingBoxPosition.z + prefab.boundingBoxSize.z / 2f);
            int heightAt = (int)GameManager.Instance.World.GetHeightAt(x, z);
            // Log.Out("Returning {0}", new Vector3(x, heightAt, z));
            return new Vector3(x, heightAt, z);
        }
        else
        {
            Vector3 loc = FindLocation(world, position, minDistance, maxDistance, target, force);
            // Log.Out("Location found {0}", loc);
            return loc;
        }

    }
    
    static public Vector3 FindLocation(World world, Vector3 position,
        float minDistance, float maxDistance, QuestTarget target,
        bool force = true, List<Vector2> used = null)
    {
        var rng = world.GetGameRandom();

        // ToDo: implement POI target here too
        Func<World, Vector3, bool> verifier = IsFlatLandOutsidePOI;
        if (target == QuestTarget.POI) verifier = IsFlatLandOutsidePOI;

        // We must only calculate on server side
        // Not sure how client gets into this
        if (!ConnectionManager.Instance.IsServer)
            Log.Error("FindLocation is Server Only!?");

        // Get the distance from default minimum and maximum range
        float distance = rng.RandomRange(minDistance, maxDistance);

        // Generate a random point to test for valid location
        Vector3i rnd = GetRandomPoint(
            world, position, distance);

        // Try multiple times for valid position
        // Enough times, but ensure no endless loop
        // As we can't guarantee a valid position
        // Traders should sort out invalid ones
        for (int n = 0; n < 75; n += 1)
        {
            // Check if this position is good for us
            if (verifier(world, rnd)) return rnd;
            // Try another random position
            rnd = GetRandomPoint(world,
                position, distance);
        }

        // Return valid position
        return force ? rnd : Vector3.zero;
    }


    //#####################################################
    //#####################################################

    static private Vector3 FindTrader(
        ref List<Tuple<byte, Vector3i>> updates,
        World world, Vector3 position,
        BiomeFilterTypes biomeFilterType = BiomeFilterTypes.AnyBiome,
        string biomeFilter = "", bool ignoreCurrentPOI = true,
        List<Vector2> exclude = null)
    {

        var provider = GameManager.Instance.World.ChunkClusters[0].ChunkProvider.GetDynamicPrefabDecorator();
        // var usedPOILocations = (UnityEngine.Object)entityPlayer != (UnityEngine.Object)null ? entityPlayer.QuestJournal.GetTraderList(factionID) : (List<Vector2>)null;
        var prefab = provider.GetClosestPOIToWorldPos(
            QuestEventManager.traderTag,
            new Vector2(position.x, position.z),
            biomeFilterType: biomeFilterType,
            ignoreCurrentPOI: ignoreCurrentPOI,
            biomeFilter: biomeFilter,
            excludeList: exclude);
        if (prefab == null) return Vector3.zero;

        updates.Add(new Tuple<byte, Vector3i>((byte)Quest.PositionDataTypes.POIPosition, prefab.boundingBoxPosition));
        updates.Add(new Tuple<byte, Vector3i>((byte)Quest.PositionDataTypes.POISize, prefab.boundingBoxSize));

        int x = (int)(prefab.boundingBoxPosition.x + prefab.boundingBoxSize.x / 2f);
        int z = (int)(prefab.boundingBoxPosition.z + prefab.boundingBoxSize.z / 2f);
        int heightAt = (int)GameManager.Instance.World.GetHeightAt(x, z);
        return new Vector3(x, heightAt, z);
    }

    //#####################################################
    // Our own implementation to keep the checks out of it
    // Gives a valid random point within world boundaries
    // Height will be adjusted to be on the world surface
    //#####################################################

    public static Vector3i GetRandomPoint(World world, Vector3 position, float distance)
    {
        Vector3i target = Vector3i.invalid;
        // Loop until the target is within bounds
        // Should be safe to produce one at some point
        // Note: there is always a chance for endless loop
        for (int i = 0; i < 1e4 && !world.IsPositionInBounds(target); i += 1)
        {
            Vector2 dir = new Vector2(world.GetGameRandom().RandomFloat * 2f + -1f,
                world.GetGameRandom().RandomFloat * 2f + -1f).normalized;
            target.x = (int)(position.x + dir.x * distance);
            target.z = (int)(position.z + dir.y * distance);
            target.y = (int)world.GetHeightAt(target.x, target.z);
        }
        // Slim chance it returns a bad result
        // Meaning it may be in water etc.
        return target;
    }

    //#####################################################
    // Check if position is valid location for an air drop
    //#####################################################

    public static bool IsFlatLandOutsidePOI(World world, Vector3 pos) =>
        // Check if it is inside valid height boundary
        (pos.y > 0 && pos.y < 256) &&
        // Check if position lies outside of world boundaries
        (world.IsPositionInBounds(pos)) &&
        // Abort if there is water at the random location
        (!world.GetWaterAt(pos.x, pos.z)) &&
        // Abort if there is no leveled ground at the random location
        (world.CheckForLevelNearbyHeights(pos.x, pos.z, 8)) &&
        // Abort if random point lies within a POI building
        (!world.IsPositionWithinPOI(pos, 15));

    //#####################################################
    //#####################################################



    public static PrefabInstance GetRandomPOINearWorldPos(
      Vector3 worldPos,
      int minSearchDistance,
      int maxSearchDistance,
      FastTags questTag,
      byte difficulty = 255,
      List<Vector2> usedPOILocations = null,
      int entityIDforQuests = -1,
      BiomeFilterTypes biomeFilterType = BiomeFilterTypes.AnyBiome,
      string biomeFilter = "")
    {
        return null;
    }

    public static PrefabInstance GetRandomPOINearWorldPos(
      Vector2 worldPos,
      int minSearchDistance,
      int maxSearchDistance,
      FastTags questTag,
      byte difficulty = 255,
      List<Vector2> usedPOILocations = null,
      int entityIDforQuests = -1,
      BiomeFilterTypes biomeFilterType = BiomeFilterTypes.AnyBiome,
      string biomeFilter = "")
    {
        List<PrefabInstance> byDifficultyTier = QuestEventManager.Current.GetPrefabsByDifficultyTier(difficulty);

        // byDifficultyTier.RemoveAll(prefab =>
        // {
        //     int x = prefab.boundingBoxPosition.x + prefab.boundingBoxSize.x / 2;
        //     int z = prefab.boundingBoxPosition.z + prefab.boundingBoxSize.z / 2;
        //     return prefab != null;
        // });
        // Log.Out(" list to select from is {0} (difficulity {1})", byDifficultyTier.Count, difficulty);

        if (byDifficultyTier == null) return null;
        string[] biomes = null;
        World world = GameManager.Instance.World;
        for (int retry = 0; retry < 3; ++retry)
        {
            for (int trial = 0; trial < 175; ++trial)
            {
                int pidx = world.GetGameRandom().RandomRange(byDifficultyTier.Count);
                PrefabInstance randomPoiNearWorldPos = byDifficultyTier[pidx];
                if (/*randomPoiNearWorldPos.prefab.bSleeperVolumes && */ randomPoiNearWorldPos.prefab.GetQuestTag(questTag) && (difficulty == 255 || randomPoiNearWorldPos.prefab.DifficultyTier == difficulty))
                {
                    //  Log.Out(" Has a prefab within difficulty tier");
                    Vector2 prefabPos = new Vector2(randomPoiNearWorldPos.boundingBoxPosition.x, randomPoiNearWorldPos.boundingBoxPosition.z);
                    if ((usedPOILocations == null || !usedPOILocations.Contains(prefabPos)) && QuestEventManager.Current.CheckForPOILockouts(entityIDforQuests, prefabPos, out ulong extraData) == QuestEventManager.POILockoutReasonTypes.None)
                    {
                        Vector2 vector2 = new Vector2(randomPoiNearWorldPos.boundingBoxPosition.x + randomPoiNearWorldPos.boundingBoxSize.x / 2f, randomPoiNearWorldPos.boundingBoxPosition.z + randomPoiNearWorldPos.boundingBoxSize.z / 2f);
                        if (biomeFilterType != BiomeFilterTypes.AnyBiome)
                        {
                            BiomeDefinition biomeAt = world.ChunkCache.ChunkProvider.GetBiomeProvider().GetBiomeAt((int)prefabPos.x, (int)prefabPos.y);
                            if (biomeFilterType != BiomeFilterTypes.OnlyBiome || !(biomeAt.m_sBiomeName != biomeFilter))
                            {
                                if (biomeFilterType == BiomeFilterTypes.ExcludeBiome)
                                {
                                    bool flag = false;
                                    if (biomes == null) biomes = biomeFilter.Split(',');
                                    for (int index3 = 0; index3 < biomes.Length; ++index3)
                                    {
                                        if (biomeAt.m_sBiomeName == biomes[index3])
                                        {
                                            flag = true;
                                            break;
                                        }
                                    }
                                    if (flag) continue;
                                }
                            }
                            else
                                continue;
                        }
                        float sqrMagnitude = (worldPos - vector2).sqrMagnitude;
                        // Log.Out("  now check if within distance {0} < {1}-{2} (at {3} vs {4})", sqrMagnitude, minSearchDistance, maxSearchDistance, vector2, worldPos);
                        if ((double)sqrMagnitude < (double)maxSearchDistance && (double)sqrMagnitude > (double)minSearchDistance)
                            return randomPoiNearWorldPos;
                    }
                }
            }
            // Expand the search radius
            minSearchDistance /= 2;
            maxSearchDistance *= 2;
        }

        return null;
    }



}

