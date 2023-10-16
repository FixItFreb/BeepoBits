using Godot;
using System;

public partial class ChannelRedeemTest : ChannelRedeemNode
{
    public override void ExecuteChannelRedeem(ChannelRedeemPayload payload)
    {
        BeepoCore.Instance.SendTwitchMessage("Thanks for the " + payload.title + " " + payload.displayname);
    }
}
