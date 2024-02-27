using Godot;
using System;

[GlobalClass]
public partial class PropRedeem : ChannelRedeemNode
{
    [Export] public StringName propName;
    [Export] float propDuration = 0;
    [Export] public StringName propAnchor;

    public override void ExecuteChannelRedeem(TwitchRedeemPayload payload)
    {
        if(CheckRedeem(payload))
        {
            PropsManager.Instance.AttachProp(propName, propAnchor, propDuration);
        }
    }
}
