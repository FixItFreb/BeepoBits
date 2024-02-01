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

    protected string[] commandParams = new string[0];

    [Signal] public delegate void OnCommandTriggerEventHandler(TwitchChatMessagePayload payload);

    public override void _EnterTree()
    {
        if (!commandName.IsNullOrEmpty())
        {
            commandNameHash = commandName.Hash();
        }
    }

    public override void _Ready()
    {
        TwitchService.Instance.ChannelChatMessage += ExecuteCommand;
    }

    public override void _ExitTree()
    {
        TwitchService.Instance.ChannelChatMessage -= ExecuteCommand;
    }

    public virtual void ExecuteCommand(TwitchChatMessagePayload payload)
    {
        if (CheckCommand(payload.data.message.text))
        {
            EmitSignal(ChatCommandNode.SignalName.OnCommandTrigger, payload);
        }
    }

    protected bool CheckCommand(string commandString)
    {
        // Is this prefixed with our command prefix and is there a command specified?
        if(commandString.Length > 1 && commandString.StartsWith(BeepoCore.Instance.commandPrefix))
        {
            // Does this match the commandName we want?
            commandString = commandString.TrimStart(BeepoCore.Instance.commandPrefix);
            string[] commandSplit = commandString.Split(' ');
            if(commandSplit[0].Hash() == commandNameHash)
            {
                if (commandString.Length > 0)
                {
                    commandParams = commandString.Split(' ');
                }
                return true;
            }
        }
        return false;
    }
}
