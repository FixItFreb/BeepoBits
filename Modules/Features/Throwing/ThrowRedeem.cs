using Godot;
using System;

public partial class ThrowRedeem : ChannelRedeemNode
{
    [Export] public int throwCount = 1;
    public override void ExecuteChannelRedeem(ChannelRedeemPayload payload)
    {
        if(payload.title == redeemTitle)
        {
            ThrowingManager.Instance.Throw(throwCount);
        }
    }
}
