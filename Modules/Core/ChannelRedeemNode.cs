using Godot;
using System;

public class ChannelRedeemPayload
{
    public string title;
    public string username;
    public string displayname;
    public string userInput;

    public ChannelRedeemPayload(string _title, string _username, string _displayname, string _userInput)
    {
        title = _title;
        username = _username;
        displayname = _displayname;
        userInput = _userInput;
    }
}

public partial class ChannelRedeemNode : Node
{
    [Export] public string redeemTitle;

    public override void _Ready()
    {
        BeepoCore.Instance.RegisterChannelRedeem(this);
    }

    public override void _ExitTree()
    {
        BeepoCore.Instance.UnregisterChannelRedeem(this);
    }

    public virtual void ExecuteChannelRedeem(ChannelRedeemPayload payload)
    {
    }
}
