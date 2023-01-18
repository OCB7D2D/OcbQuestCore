using System.Collections.Generic;

//###########################################################################
//###########################################################################

public struct HordeSpawnerCfg
{

    //#####################################################
    //#####################################################

    public int PlayerID;
    public int TargetID;

    public int NeededKills;
    public int MaxEntityCount;
    public int SpawnExcessCount;

    public float MinSpawnWait;
    public float MaxSpawnWait;

    public float MinPlayerDistance;
    public float MaxPlayerDistance;
    public float EntityHeightOffset;

    public float TargetChance;
    public float ReTargetChance;
    public float PlayerTargetChance;
    public float PlayerReTargetChance;

    // API only supports int
    public int MinTargetTime;
    public int MaxTargetTime;
    public int MinReTargetTime;
    public int MaxReTargetTime;

    public float ReTargetMinDelay;
    public float ReTargetMaxDelay;

    public float ReRageMinDelay;
    public float ReRageMaxDelay;

    public float RageChance;
    public float ReRageChance;
    public float MinRageSpeed;
    public float MaxRageSpeed;
    public float MinRageTime;
    public float MaxRageTime;
    public float MinReRageSpeed;
    public float MaxReRageSpeed;
    public float MinReRageTime;
    public float MaxReRageTime;

    public List<string> SpawnerGroups;

    //#####################################################
    //#####################################################

    public HordeSpawnerCfg(

        int PlayerID = -1,
        int TargetID = -1,
        int NeededKills = 12,
        int MaxEntityCount = 4,
        int SpawnExcessCount = 2,
        float MinSpawnWait = 1.5f,
        float MaxSpawnWait = 4.5f,
        float MinPlayerDistance = 12f,
        float MaxPlayerDistance = 34f,
        float EntityHeightOffset = 1.5f,

        float TargetChance = 0.75f,
        float ReTargetChance = 0.75f,
        float PlayerTargetChance = 0.125f,
        float PlayerReTargetChance = 0.125f,

        float ReTargetMinDelay = 0.75f,
        float ReTargetMaxDelay = 1.25f,

        float ReRageMinDelay = 2.75f,
        float ReRageMaxDelay = 5.25f,

        int MinTargetTime = 15,
        int MaxTargetTime = 90,
        int MinReTargetTime = 10,
        int MaxReTargetTime = 45,

        float RageChance = 0.25f,
        float MinRageSpeed = 1.5f,
        float MaxRageSpeed = 2.5f,
        float MinRageTime = 12f,
        float MaxRageTime = 24f,
        float ReRageChance = 0.0075f,
        float MinReRageSpeed = 1.5f,
        float MaxReRageSpeed = 2.5f,
        float MinReRageTime = 12f,
        float MaxReRageTime = 24f
        )
    {
        this.PlayerID = PlayerID;
        this.TargetID = TargetID;
        this.NeededKills = NeededKills;
        this.MaxEntityCount = MaxEntityCount;
        this.SpawnExcessCount = SpawnExcessCount;
        this.MinSpawnWait = MinSpawnWait;
        this.MaxSpawnWait = MaxSpawnWait;

        this.MinPlayerDistance = MinPlayerDistance;
        this.MaxPlayerDistance = MaxPlayerDistance;
        this.EntityHeightOffset = EntityHeightOffset;

        this.TargetChance = TargetChance;
        this.ReTargetChance = ReTargetChance;
        this.PlayerTargetChance = PlayerTargetChance;
        this.PlayerReTargetChance = PlayerReTargetChance;

        this.ReTargetMinDelay = ReTargetMinDelay;
        this.ReTargetMaxDelay = ReTargetMaxDelay;

        this.ReRageMinDelay = ReRageMinDelay;
        this.ReRageMaxDelay = ReRageMaxDelay;

        this.MinTargetTime = MinTargetTime;
        this.MaxTargetTime = MaxTargetTime;
        this.MinReTargetTime = MinReTargetTime;
        this.MaxReTargetTime = MaxReTargetTime;

        this.RageChance = RageChance;
        this.MinRageSpeed = MinRageSpeed;
        this.MaxRageSpeed = MaxRageSpeed;
        this.MinRageTime = MinRageTime;
        this.MaxRageTime = MaxRageTime;

        this.ReRageChance = ReRageChance;
        this.MinReRageSpeed = MinReRageSpeed;
        this.MaxReRageSpeed = MaxReRageSpeed;
        this.MinReRageTime = MinReRageTime;
        this.MaxReRageTime = MaxReRageTime;

        SpawnerGroups = new List<string>();
    }

    //#####################################################
    //#####################################################

    public void ParseProperties(DynamicProperties properties)
    {

        if (properties.Values.ContainsKey("value"))
            NeededKills = properties.GetInt("value");

        if (properties.Values.ContainsKey("max_entity_count"))
            MaxEntityCount = properties.GetInt("max_entity_count");
        if (properties.Values.ContainsKey("spawn_excess_count"))
            SpawnExcessCount = properties.GetInt("spawn_excess_count");

        if (properties.Values.ContainsKey("min_player_distance"))
            MinPlayerDistance = properties.GetFloat("min_player_distance");
        if (properties.Values.ContainsKey("max_player_distance"))
            MaxPlayerDistance = properties.GetFloat("max_player_distance");
        if (properties.Values.ContainsKey("entity_height_offset"))
            EntityHeightOffset = properties.GetFloat("entity_height_offset");

        if (properties.Values.ContainsKey("min_spawn_wait"))
            MinSpawnWait = properties.GetFloat("min_spawn_wait");
        if (properties.Values.ContainsKey("max_spawn_wait"))
            MaxSpawnWait = properties.GetFloat("max_spawn_wait");

        // Target settings

        if (properties.Values.ContainsKey("target_chance"))
            TargetChance = properties.GetFloat("target_chance");
        if (properties.Values.ContainsKey("player_target_chance"))
            PlayerTargetChance = properties.GetFloat("player_target_chance");

        if (properties.Values.ContainsKey("re_target_chance"))
            ReTargetChance = properties.GetFloat("re_target_chance");
        if (properties.Values.ContainsKey("min_target_time"))
            MinTargetTime = properties.GetInt("min_target_time");
        if (properties.Values.ContainsKey("max_target_time"))
            MaxTargetTime = properties.GetInt("max_target_time");

        ReTargetChance = TargetChance;
        MinReTargetTime = MinTargetTime;
        MaxReTargetTime = MaxTargetTime;

        if (properties.Values.ContainsKey("re_target_min_delay"))
            ReTargetMinDelay = properties.GetFloat("re_target_min_delay");
        if (properties.Values.ContainsKey("re_target_max_delay"))
            ReTargetMaxDelay = properties.GetFloat("re_target_max_delay");

        if (properties.Values.ContainsKey("player_re_target_chance"))
            PlayerReTargetChance = properties.GetFloat("player_re_target_chance");
        if (properties.Values.ContainsKey("min_re_target_time"))
            MinReTargetTime = properties.GetInt("min_re_target_time");
        if (properties.Values.ContainsKey("max_re_target_time"))
            MaxReTargetTime = properties.GetInt("max_re_target_time");

        // Rage settings

        if (properties.Values.ContainsKey("rage_chance"))
            RageChance = properties.GetFloat("rage_chance");
        if (properties.Values.ContainsKey("min_rage_speed"))
            MinRageSpeed = properties.GetFloat("min_rage_speed");
        if (properties.Values.ContainsKey("max_rage_speed"))
            MaxRageSpeed = properties.GetFloat("max_rage_speed");
        if (properties.Values.ContainsKey("min_rage_time"))
            MinRageTime = properties.GetFloat("min_rage_time");
        if (properties.Values.ContainsKey("max_rage_time"))
            MaxRageTime = properties.GetFloat("max_rage_time");

        ReRageChance = RageChance;
        MinReRageSpeed = MinRageSpeed;
        MaxReRageSpeed = MaxRageSpeed;
        MinReRageTime = MinRageTime;
        MaxReRageTime = MaxRageTime;

        if (properties.Values.ContainsKey("re_rage_min_delay"))
            ReRageMinDelay = properties.GetFloat("re_rage_min_delay");
        if (properties.Values.ContainsKey("re_rage_max_delay"))
            ReRageMaxDelay = properties.GetFloat("re_rage_max_delay");

        if (properties.Values.ContainsKey("re_rage_chance"))
            ReRageChance = properties.GetFloat("re_rage_chance");
        if (properties.Values.ContainsKey("min_re_rage_speed"))
            MinReRageSpeed = properties.GetFloat("min_re_rage_speed");
        if (properties.Values.ContainsKey("max_re_rage_speed"))
            MaxReRageSpeed = properties.GetFloat("max_re_rage_speed");
        if (properties.Values.ContainsKey("min_re_rage_time"))
            MinReRageTime = properties.GetFloat("min_re_rage_time");
        if (properties.Values.ContainsKey("max_re_rage_time"))
            MaxReRageTime = properties.GetFloat("max_re_rage_time");

    }

    //#####################################################
    //#####################################################

    public void CopyFrom(HordeSpawnerCfg from)
    {
        PlayerID = from.PlayerID;
        TargetID = from.TargetID;
        SpawnerGroups = new List<string>();
        from.SpawnerGroups?.CopyTo(SpawnerGroups);
        NeededKills = from.NeededKills;
        MaxEntityCount = from.MaxEntityCount;
        SpawnExcessCount = from.SpawnExcessCount;
        MinSpawnWait = from.MinSpawnWait;
        MaxSpawnWait = from.MaxSpawnWait;
        MinPlayerDistance = from.MinPlayerDistance;
        MaxPlayerDistance = from.MaxPlayerDistance;
        EntityHeightOffset = from.EntityHeightOffset;
        RageChance = from.RageChance;
        ReRageChance = from.ReRageChance;
        MinRageSpeed = from.MinRageSpeed;
        MaxRageSpeed = from.MaxRageSpeed;
        MinRageTime = from.MinRageTime;
        MaxRageTime = from.MaxRageTime;
        TargetChance = from.TargetChance;
        ReTargetChance = from.ReTargetChance;
        PlayerTargetChance = from.PlayerTargetChance;
        PlayerReTargetChance = from.PlayerReTargetChance;

        ReTargetMinDelay = from.ReTargetMinDelay;
        ReTargetMaxDelay = from.ReTargetMaxDelay;

        ReRageMinDelay = from.ReRageMinDelay;
        ReRageMaxDelay = from.ReRageMaxDelay;

        //ReTargetChance = from.ReTargetChance;
        //RePlayerTargetChance = from.RePlayerTargetChance;

        MinTargetTime = from.MinTargetTime;
        MaxTargetTime = from.MaxTargetTime;
        MinReTargetTime = from.MinReTargetTime;
        MaxReTargetTime = from.MaxReTargetTime;
    }

    //#####################################################
    //#####################################################

    public void AddSpawnGroup(string group)
    {
        if (SpawnerGroups == null)
            SpawnerGroups = new List<string>();
        if (!string.IsNullOrEmpty(group?.Trim()))
            SpawnerGroups.Add(group.Trim());
    }

    //#####################################################
    //#####################################################

    public void ReadFrom(PooledBinaryReader br)
    {
        PlayerID = br.ReadInt32();
        TargetID = br.ReadInt32();
        int count = br.ReadInt32();
        SpawnerGroups = new List<string>();
        for (int i = 0; i < count; i++)
            SpawnerGroups.Add(br.ReadString());
        NeededKills = br.ReadInt32();
        MaxEntityCount = br.ReadInt32();
        SpawnExcessCount = br.ReadInt32();
        MinSpawnWait = br.ReadSingle();
        MaxSpawnWait = br.ReadSingle();
        MinPlayerDistance = br.ReadSingle();
        MaxPlayerDistance = br.ReadSingle();
        EntityHeightOffset = br.ReadSingle();
        RageChance = br.ReadSingle();
        ReRageChance = br.ReadSingle();
        MinRageSpeed = br.ReadSingle();
        MaxRageSpeed = br.ReadSingle();
        MinRageTime = br.ReadSingle();
        MaxRageTime = br.ReadSingle();
        TargetChance = br.ReadSingle();
        ReTargetChance = br.ReadSingle();
        PlayerTargetChance = br.ReadSingle();
        PlayerReTargetChance = br.ReadSingle();

        ReTargetMinDelay = br.ReadSingle();
        ReTargetMaxDelay = br.ReadSingle();

        ReRageMinDelay = br.ReadSingle();
        ReRageMaxDelay = br.ReadSingle();

        //RePlayerTargetChance = br.ReadSingle();
        MinTargetTime = br.ReadInt32();
        MaxTargetTime = br.ReadInt32();
        MinReTargetTime = br.ReadInt32();
        MaxReTargetTime = br.ReadInt32();
    }

    public void WriteTo(PooledBinaryWriter bw)
    {
        bw.Write(PlayerID);
        bw.Write(TargetID);
        bw.Write(SpawnerGroups.Count);
        for(int i = 0;i < SpawnerGroups.Count;i++)
            bw.Write(SpawnerGroups[i]);
        bw.Write(NeededKills);
        bw.Write(MaxEntityCount);
        bw.Write(SpawnExcessCount);
        bw.Write(MinSpawnWait);
        bw.Write(MaxSpawnWait);

        bw.Write(MinPlayerDistance);
        bw.Write(MaxPlayerDistance);
        bw.Write(EntityHeightOffset);
        bw.Write(RageChance);
        bw.Write(ReRageChance);
        bw.Write(MinRageSpeed);
        bw.Write(MaxRageSpeed);
        bw.Write(MinRageTime);
        bw.Write(MaxRageTime);
        bw.Write(TargetChance);
        bw.Write(ReTargetChance);
        bw.Write(PlayerTargetChance);
        bw.Write(PlayerReTargetChance);

        bw.Write(ReTargetMinDelay);
        bw.Write(ReTargetMaxDelay);

        bw.Write(ReRageMinDelay);
        bw.Write(ReRageMaxDelay);

        bw.Write(MinTargetTime);
        bw.Write(MaxTargetTime);
        bw.Write(MinReTargetTime);
        bw.Write(MaxReTargetTime);
    }

    //#####################################################
    //#####################################################

}
