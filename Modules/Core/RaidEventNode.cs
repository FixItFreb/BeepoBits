using Godot;
using System;

public partial class RaidEventNode : Node
{
    [Signal] public delegate void OnRaidTriggerEventHandler(TwitchRaidPayload payload);

    public override void _Ready()
    {
        //TwitchService.Instance.ChannelRaid += ExecuteRaidEvent;
        BeepoCore.Instance.RegisterRaidEvent(this);
    }

    public override void _ExitTree()
    {
        //TwitchService.Instance.ChannelRaid -= ExecuteRaidEvent;
        BeepoCore.Instance.UnregisterRaidEvent(this);
    }

    public virtual void ExecuteRaidEvent(TwitchRaidPayload payload)
    {
        EmitSignal(RaidEventNode.SignalName.OnRaidTrigger, payload);
    }
}
