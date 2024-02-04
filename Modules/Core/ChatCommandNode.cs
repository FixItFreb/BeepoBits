using Godot;
using System;

[GlobalClass]
public partial class ChatCommandNode : Node
{
    protected string commandName;
    [Export] public string CommandName
    {
        get { return commandName; }
        set
        {
            if(commandName != value)
            {
                commandName = value;
                commandNameHash = commandName.Hash();
            }
        }
    }

    public uint commandNameHash { get; private set; }

    [Export] public TwitchBadge requiredBadges;

    //public string[] commandParams = new string[0];

    [Signal] public delegate void OnCommandTriggerEventHandler(TwitchChatMessagePayload payload, string[] commandParams);

    public override void _EnterTree()
    {
        if (!commandName.IsNullOrEmpty())
        {
            commandNameHash = commandName.Hash();
        }
    }

    public override void _Ready()
    {
        //TwitchService.Instance.ChannelChatMessage += ExecuteCommand;
        BeepoCore.Instance.RegisterChatCommand(this);
    }

    public override void _ExitTree()
    {
        //TwitchService.Instance.ChannelChatMessage -= ExecuteCommand;
        BeepoCore.Instance.UnregisterChatCommand(this);
    }

    public virtual void ExecuteCommand(TwitchChatMessagePayload payload, string[] commandParams)
    {
        if (CheckPermissions(payload))
        {
            EmitSignal(ChatCommandNode.SignalName.OnCommandTrigger, payload, commandParams);
        }
    }

    protected bool CheckPermissions(TwitchChatMessagePayload payload)
    {
        if(requiredBadges == TwitchBadge.None)
        {
            return true;
        }
        return false;
    }
}
