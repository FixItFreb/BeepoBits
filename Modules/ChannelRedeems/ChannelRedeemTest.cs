using Godot;
using System;

public partial class ChannelRedeemTest : ChannelRedeemNode
{
    public override void ExecuteChannelRedeem(ChannelRedeemPayload payload)
    {
        beepoCore.SendTwitchMessage("Thanks for the " + payload.title + " " + payload.displayname);
    }
}
