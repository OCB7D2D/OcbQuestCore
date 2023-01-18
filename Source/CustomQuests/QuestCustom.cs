using HarmonyLib;
using Quests.Requirements;
using System.Collections.Generic;
using System.Xml.Linq;
using UnityEngine;

//###########################################################################
// A custom quest that doesn't do much on it own. You may use
// it for static quests where you use my new quest objectives.
//###########################################################################

public interface IOcbQuestCreator
{
    void ParseXmlConfig(XElement cfg);
    QuestCustom CreateQuest(QuestClass klass);
}

//#####################################################
//#####################################################

public class QuestCustom : Quest
{

    //#####################################################
    // Required for the activator to work
    //#####################################################

    public QuestCustom()
        : base(string.Empty)
    {
    }

    //#####################################################
    // Not really used, but still needed
    //#####################################################

    public QuestCustom(string id)
        : base(id)
    {
        InitAfterCtor(id);
    }

    //#####################################################
    //#####################################################

    // Property is read-only, so this is the way to still set it
    static readonly HarmonyPropertyProxy<string> IdPropertyProxy =
        new HarmonyPropertyProxy<string>(typeof(Quest), "ID");

    // We are created via activator (from custom class string)
    // Therefore we need a method to init after construction
    virtual public void InitAfterCtor(string id)
    {
        IdPropertyProxy.Set(this, id);
        CurrentState = QuestState.InProgress;
        CurrentPhase = 1;
    }

    //#####################################################
    // Execute the same code as when quest are created in vanilla
    // This basically copies the config from class to actual quest
    //#####################################################

    virtual public void InitVanillaQuest(QuestClass klass)
    {
        CurrentQuestVersion = klass.CurrentVersion;
        // Tracked = false;
        PreviousQuest = klass.PreviousQuest;
        // FinishTime = 0UL;
        QuestFaction = klass.QuestFaction;
        for (int i = 0; i < klass.Actions.Count; ++i)
        {
            BaseQuestAction baseQuestAction = klass.Actions[i].Clone();
            baseQuestAction.OwnerQuest = this;
            Actions.Add(baseQuestAction);
        }
        for (int i = 0; i < klass.Requirements.Count; ++i)
        {
            BaseRequirement baseRequirement = klass.Requirements[i].Clone();
            baseRequirement.OwnerQuest = this;
            Requirements.Add(baseRequirement);
        }
        for (int i = 0; i < klass.Objectives.Count; ++i)
        {
            BaseObjective copy = klass.Objectives[i].Clone();
            copy.OwnerQuest = this;
            Objectives.Add(copy);
        }
        for (int i = 0; i < klass.Rewards.Count; ++i)
        {
            BaseReward baseReward = klass.Rewards[i].Clone();
            baseReward.OwnerQuest = this;
            Rewards.Add(baseReward);
        }
    }

    //#####################################################
    //#####################################################

    static readonly HarmonyPropertyProxy<string> IdProxy =
        new HarmonyPropertyProxy<string>(typeof(Quest), "ID");

    static readonly HarmonyPropertyProxy<bool> OptionalCompleteProxy =
        new HarmonyPropertyProxy<bool>(typeof(Quest), "OptionalComplete");

    public virtual void CopyFrom(QuestCustom from)
    {
        IdProxy.Set(this, from.ID);
        OwnerJournal = from.OwnerJournal;
        CurrentQuestVersion = from.CurrentQuestVersion;
        CurrentState = from.CurrentState;
        FinishTime = from.FinishTime;
        SharedOwnerID = from.SharedOwnerID;
        QuestGiverID = from.QuestGiverID;
        CurrentPhase = from.CurrentPhase;
        QuestCode = from.QuestCode;
        RallyMarkerActivated = from.RallyMarkerActivated;
        Tracked = from.Tracked;
        OptionalCompleteProxy.Set(this, from.OptionalComplete);
        QuestTags = from.QuestTags;
        TrackingHelper = from.TrackingHelper;
        QuestFaction = from.QuestFaction;
        for (int i = 0; i < from.Actions.Count; ++i)
        {
            BaseQuestAction action = from.Actions[i].Clone();
            action.OwnerQuest = this;
            Actions.Add(action);
        }
        for (int i = 0; i < from.Requirements.Count; ++i)
        {
            BaseRequirement requirement = from.Requirements[i].Clone();
            requirement.OwnerQuest = this;
            Requirements.Add(requirement);
        }
        for (int i = 0; i < from.Objectives.Count; ++i)
        {
            BaseObjective objective = from.Objectives[i].Clone();
            objective.OwnerQuest = this;
            Objectives.Add(objective);
        }
        for (int i = 0; i < from.Rewards.Count; ++i)
        {
            BaseReward reward = from.Rewards[i].Clone();
            reward.OwnerQuest = this;
            Rewards.Add(reward);
        }
        foreach (KeyValuePair<string, string> dataVariable in from.DataVariables)
            DataVariables.Add(dataVariable.Key, dataVariable.Value);
        foreach (KeyValuePair<PositionDataTypes, Vector3> keyValuePair in from.PositionData)
            PositionData.Add(keyValuePair.Key, keyValuePair.Value);
    }

    //#####################################################
    //#####################################################

    public virtual new Quest Clone()
    {
        var quest = new QuestCustom();
        quest.CopyFrom(this);
        return quest;
    }

    //########################################################
    //########################################################

    [HarmonyPatch(typeof(Quest), "Clone")]
    private class QuestClone
    {
        static bool Prefix(Quest __instance, ref Quest __result)
        {
            if (__instance is QuestCustom custom)
            {
                __result = custom.Clone();
                return false;
            }
            return true;
        }
    }

    //#####################################################
    //#####################################################

    virtual public string GetVariableText(string field, int idx, string name)
    {
        return "[NA]";
    }

    //#####################################################
    //#####################################################

}
