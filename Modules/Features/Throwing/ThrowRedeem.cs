using Godot;
using System;

public partial class ThrowRedeem : ChannelRedeemNode
{
    [Export] public int throwCount = 1;
    public override void ExecuteChannelRedeem(TwitchRedeemPayload payload)
    {
        if (payload.data.reward.title.Hash() == titleHash)
        {
            ThrowingManager.Instance.Throw(throwCount);
        }
    }
}
