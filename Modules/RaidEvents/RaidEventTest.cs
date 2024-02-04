using Godot;
using System;

public partial class RaidEventTest : RaidEventNode
{
    public override void ExecuteRaidEvent(TwitchRaidPayload payload)
    {
        BeepoCore.SendTwitchMessage("Hey " + payload.data.from_broadcaster_user_name + " thanks for bringing " + payload.data.viewers + " beans to the party!");
    }
}
