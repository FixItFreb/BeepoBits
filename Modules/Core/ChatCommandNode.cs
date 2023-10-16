using Godot;
using System;

public class ChatCommandPayload
{
    public string username;
    public string displayname;
    public string message;
    public int bits;
    public int badges;

    public ChatCommandPayload(string _username, string _displayname, string _message, int _bits, int _badges)
    {
        username = _username;
        displayname = _displayname;
        message = _message;
        bits = _bits;
        badges = _badges;
    }
}

public partial class ChatCommandNode : Node
{
    [Export] public string commandName;
    [Export] public TwitchBadge requiredBadges;

    public override void _Ready()
    {
        BeepoCore.Instance.RegisterChatCommand(this);
    }

    public override void _ExitTree()
    {
        BeepoCore.Instance.UnregisterChatCommand(this);
    }

    public virtual void ExecuteCommand(ChatCommandPayload payload)
    {
    }
}
