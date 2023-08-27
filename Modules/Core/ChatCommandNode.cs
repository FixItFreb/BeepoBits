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

    protected BeepoCore beepoCore;

    public override void _Ready()
    {
        beepoCore = GetNode<BeepoCore>("%BeepoCore");
        beepoCore.RegisterChatCommand(this);
    }

    public override void _ExitTree()
    {
        beepoCore.UnregisterChatCommand(this);
    }

    public virtual void ExecuteCommand(ChatCommandPayload payload)
    {
    }
}
