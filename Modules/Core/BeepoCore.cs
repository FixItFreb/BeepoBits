using Godot;
using GDC = Godot.Collections;
using System;
using System.Collections.Generic;

public partial class BeepoCore : Node
{
    private static BeepoCore _instance;
    public static BeepoCore Instance { get { return _instance; } }

    [Export] private char commandPrefix = '!';
    [Export] private TwitchService twitchService;
    [Export] private TextEdit debugTextWindow;

    private GDC.Dictionary<string, ChatCommandNode> chatCommands = new GDC.Dictionary<string, ChatCommandNode>();
    private GDC.Dictionary<string, ChannelRedeemNode> channelRedeems = new GDC.Dictionary<string, ChannelRedeemNode>();
    private List<RaidEventNode> raidEvents = new List<RaidEventNode>();

    public static List<BeepoAvatar> currentAvatars = new List<BeepoAvatar>();

    [Signal] public delegate void NewAvatarRegisteredEventHandler(BeepoAvatar newAvatar);
    [Signal] public delegate void AvatarUnregisteredEventHandler(BeepoAvatar avatar);

    public override void _EnterTree()
    {
        _instance = this;
    }

    public override void _Ready()
    {
        twitchService.ChannelPointsRedeem += OnRedeemTriggered;
        twitchService.ChannelChatMessage += OnMessageTriggered;
        twitchService.ChannelRaid += OnRaidTriggered;
    }

    private void OnRedeemTriggered(string title, string username, string displayname, string userInput)
    {
        string debugString = string.Format("{4} : Redeem: {0}, Username: {1}, Displayname: {2}, Input: {3}", title, username, displayname, userInput, Time.GetTimeStringFromSystem());
        GD.Print(debugString);
        debugTextWindow.Text += string.Format("{0}\n", debugString);
        debugTextWindow.ScrollVertical = Int32.MaxValue;

        if(channelRedeems.TryGetValue(title, out ChannelRedeemNode redeem))
        {
            redeem.ExecuteChannelRedeem(new ChannelRedeemPayload(title, username, displayname, userInput));
        }
    }

    private void OnRaidTriggered(string raiderUsername, string raiderDisplayName, string raiderUserCount)
    {
        string debugString = string.Format("{3} : RaiderUsername: {0}, RaiderDisplayName: {1}, RaidCount: {2}", raiderUsername, raiderDisplayName, raiderUserCount, Time.GetTimeStringFromSystem());
        GD.Print(debugString);
        debugTextWindow.Text += string.Format("{0}\n", debugString);
        debugTextWindow.ScrollVertical = Int32.MaxValue;

        foreach(RaidEventNode r in raidEvents)
        {
            r.ExecuteRaidEvent(new RaidEventPayload(raiderUsername, raiderDisplayName, raiderUserCount));
        }
    }

    private void OnMessageTriggered(string fromUsername, string fromDisplayName, string message, int bits, int badges)
    {
        string debugString = string.Format("{4} : FromUsername: {0}, FromDisplayName: {1}, Message: {2}, Bits: {3}", fromUsername, fromDisplayName, message, bits, Time.GetTimeStringFromSystem());
        GD.Print(debugString);
        debugTextWindow.Text += string.Format("{0}\n", debugString);
        debugTextWindow.ScrollVertical = Int32.MaxValue;

        if(message[0] == commandPrefix)
        {
            string[] splitMessage = message.Substring(1).Split(' ');
            if (splitMessage.Length > 0)
            {
                string commandString = splitMessage[0];
                if(chatCommands.TryGetValue(commandString, out ChatCommandNode chatCommand))
                {
                    if ((int)chatCommand.requiredBadges == 0 || (badges & (int)chatCommand.requiredBadges) != 0)
                    {
                        chatCommand.ExecuteCommand(new ChatCommandPayload(fromUsername, fromDisplayName, message, bits, badges));
                    }
                }
            }
        }
    }

    public void RegisterChatCommand(ChatCommandNode toAdd)
    {
        if (!chatCommands.ContainsKey(toAdd.commandName))
        {
            chatCommands.Add(toAdd.commandName, toAdd);
        }
    }

    public void UnregisterChatCommand(ChatCommandNode toRemove)
    {
        chatCommands.Remove(toRemove.commandName);
    }

    public void RegisterRaidEvent(RaidEventNode toAdd)
    {
        raidEvents.Add(toAdd);
    }

    public void UnregisterRaidEvent(RaidEventNode toRemove)
    {
        raidEvents.Remove(toRemove);
    }

    public void RegisterChannelRedeem(ChannelRedeemNode toAdd)
    {
        if(!channelRedeems.ContainsKey(toAdd.redeemTitle))
        {
            channelRedeems.Add(toAdd.redeemTitle, toAdd);
        }
    }

    public void UnregisterChannelRedeem(ChannelRedeemNode toRemove)
    {
        channelRedeems.Remove(toRemove.redeemTitle);
    }

    public bool HasBadge(int badges, TwitchBadge badge)
    {
        return (badges & (int)badge) != 0;
    }

    public void SendTwitchMessage(string newMessage)
    {
        twitchService.twitchServiceIRC.ClientIRCSend("PRIVMSG #" + twitchService.twitchUsername + " :" + newMessage);
    }

    public static void RegisterNewAvatar(BeepoAvatar newAvatar)
    {
        if(currentAvatars.Contains(newAvatar))
        {
            return;
        }

        currentAvatars.Add(newAvatar);
        _instance.EmitSignal(BeepoCore.SignalName.NewAvatarRegistered, newAvatar);
    }

    public static void UnregisterAvatar(BeepoAvatar avatar)
    {
        if (currentAvatars.Contains(avatar))
        {
            currentAvatars.Remove(avatar);
            _instance.EmitSignal(BeepoCore.SignalName.AvatarUnregistered, avatar);
        }
    }
}
