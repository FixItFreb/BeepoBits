using Godot;
using System;

public partial class BodyMorphRedeem : ChannelRedeemNode
{
    [Export] public StringName bodyMorphSetName;
    public override void ExecuteChannelRedeem(TwitchRedeemPayload payload)
    {
        if(CheckRedeem(payload))
        {
            BodyMorphManager.Instance.TriggerSet(bodyMorphSetName);
        }
    }
}
