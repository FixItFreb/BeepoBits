using Godot;
using System;

[GlobalClass]
public partial class ChannelRedeemNode : Node
{
    protected string redeemTitle;
    [Export]
    public string RedeemTitle
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
        //TwitchService.Instance.ChannelPointsRedeem += ExecuteChannelRedeem;
        BeepoCore.Instance.RegisterChannelRedeem(this);
    }

    public override void _ExitTree()
    {
        //TwitchService.Instance.ChannelPointsRedeem -= ExecuteChannelRedeem;
        BeepoCore.Instance.UnregisterChannelRedeem(this);
    }

    public virtual void ExecuteChannelRedeem(TwitchRedeemPayload payload)
    {
        if (!CheckRedeem(payload))
        {
            return;
        }
        EmitSignal(ChannelRedeemNode.SignalName.OnRedeemTrigger, payload);
    }

    public bool CheckRedeem(TwitchRedeemPayload payload)
    {
        return !redeemTitle.IsNullOrEmpty() && payload.data.reward.title.Hash() == titleHash;
    }
}
