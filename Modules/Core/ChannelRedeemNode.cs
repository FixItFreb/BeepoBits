using Godot;
using System;

[GlobalClass]
public partial class ChannelRedeemNode : Node
{
    protected string redeemTitle;
    [Export] public string RedeemTitle
    {
        get { return redeemTitle; }
        set
        {
            if (value != redeemTitle)
            {
                redeemTitle = value;
                titleHash = redeemTitle.Hash();
            }
        }
    }

    protected uint titleHash = 0;

    [Signal] public delegate void OnRedeemTriggerEventHandler(TwitchRedeemPayload payload);

    public override void _EnterTree()
    {
        if (!redeemTitle.IsNullOrEmpty())
        {
            titleHash = redeemTitle.Hash();
        }
    }

    public override void _Ready()
    {
        TwitchService.Instance.ChannelPointsRedeem += ExecuteChannelRedeem;
    }

    public override void _ExitTree()
    {
        TwitchService.Instance.ChannelPointsRedeem -= ExecuteChannelRedeem;
    }

    public virtual void ExecuteChannelRedeem(TwitchRedeemPayload payload)
    {
        if (redeemTitle.IsNullOrEmpty() || payload.data.reward.title.Hash() == titleHash)
        {
            EmitSignal(ChannelRedeemNode.SignalName.OnRedeemTrigger, payload);
        }
    }
}
