using Godot;
using System;

public partial class RaidEventTest : RaidEventNode
{
    public override void ExecuteRaidEvent(RaidEventPayload payload)
    {
        beepoCore.SendTwitchMessage("Hey " + payload.raiderDisplayname + " thanks for bringing " + payload.raidersCount + " beans to the party!");
    }
}
