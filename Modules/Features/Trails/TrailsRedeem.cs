using Godot;
using System;

public partial class TrailsRedeem : ChannelRedeemNode
{
    [Export] public StringName[] trailSetsToTrigger = new StringName[0];
    [Export] public float trailSetDuration = 0f;

    public override void ExecuteChannelRedeem(TwitchRedeemPayload payload)
    {
        if(!CheckRedeem(payload))
        {
            return;
        }

        foreach (StringName s in trailSetsToTrigger)
        {
            TrailsManager.Instance.TriggerSet(s, trailSetDuration);
        }
    }
}
