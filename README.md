# OCB More Quests Mod - 7 Days to Die Addon

Note: readme is not finished and just a wild write up for now!

## Quest Setup and Multi-Player

NPC quests, offered to players, are created on the server side.
These are sent to the clients via `NetPackageNPCQuestList`.
Each of these quest consists of a only few properties:
The `ID` tells each client what objectives to do etc.

```
string QuestID = quest.ID;
Vector3 QuestLocation = quest.GetLocation();
Vector3 QuestSize = quest.GetLocationSize();
string POIName = activeQuest.QuestPrefab.name;
Vector3 TraderPos = TraderNPC.position;
```

Once the data is sent to clients, the actual `Quest` object
is re-created also on the client-side. No further details
are shared between server and client, thus the resulting
Quests my differ from client to client, if anything in
them is random.

Once a quest is started, it is persisted at `PlayerDataFile`.
The number and types of objects must not change, as the
persisted data is tightly coupled to the structure of
the quest (and objective types are not persisted).

We can still use `PositionData` to store any `Vector3`
within the main Quest data (only for `InProgress`). For
anything else we can use `DataVariables`, although this
limits anything to be stored as a `string`.

## Accepting trader quests

Once you accept a quest at the trader, you only have the
minimal information from the server (see above). We know
mainly the start position. First the actual quest is
created or generated via `CreateQuest` from `QuestID`.
Then we use `SetPosition` to call on each objective:

```
OwnerQuest = this;
HandleVariables();
SetupQuestTag();
// In a second loop afterwards
SetPosition(position, size);
```

## About quests and start location

When quests are offered at the trader, it shows the distance
to the starting point (plus direction). In order for that to
work in MP, the position must explicitly be set by an objective.
It works correctly in SP also if any of the objectives just
set some `PositionData`. Use objective `RandomPlace` in order
to have the start position set explicitly by that objective.

## Changelog

### Version 0.0.1

- Initial working version

## Compatibility

I've developed and tested this Mod against version a21.1(b16)

[1]: https://github.com/OCB7D2D/ElectricityOverhaul
[2]: https://docs.unity3d.com/2017.2/Documentation/Manual/SpecialFolders.html