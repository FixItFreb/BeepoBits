using Godot;
using System;

public partial class ChannelRedeemTest : ChannelRedeemNode
{
    public override void ExecuteChannelRedeem(TwitchRedeemPayload payload)
    {
        BeepoCore.SendTwitchMessage("Thanks for the " + payload.data.reward.title + " " + payload.data.user_name);
        BeepoCore.DebugLog("Thanks for the " + payload.data.reward.title + " " + payload.data.user_name);
    }
}
