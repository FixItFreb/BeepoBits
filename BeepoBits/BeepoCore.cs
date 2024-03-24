using Godot;
using GDC = Godot.Collections;
using System;
using System.Collections.Generic;

public partial class BeepoCore : Node
{
    private static BeepoCore _instance;
    public static BeepoCore Instance { get { return _instance; } }

    [Export] public char commandPrefix = '!';
    [Export] private TwitchService twitchService;

    private GDC.Dictionary<string, ChatCommandNode> chatCommands = new GDC.Dictionary<string, ChatCommandNode>();
    private GDC.Dictionary<string, ChannelRedeemNode> channelRedeems = new GDC.Dictionary<string, ChannelRedeemNode>();
    private List<RaidEventNode> raidEvents = new List<RaidEventNode>();

    public static List<BeepoAvatar> currentAvatars = new List<BeepoAvatar>();

    private static Node3D avatarAnchor;
    public static Node3D AvatarAnchor { get { return avatarAnchor; } }
    private static Node3D worldRoot;
    public static Node3D WorldRoot { get { return worldRoot; } }

    public List<IEventDomain> eventDomains = new List<IEventDomain>();

    [Signal] public delegate void NewAvatarRegisteredEventHandler(BeepoAvatar newAvatar);
    [Signal] public delegate void AvatarUnregisteredEventHandler(BeepoAvatar avatar);
    [Signal] public delegate void OnDebugLogEventHandler(string debugString);

    public static Camera3D CurrentCamera { get { return _instance.GetViewport().GetCamera3D(); } }

    public override void _EnterTree()
    {
        _instance = this;
    }

    public override void _Ready()
    {
        twitchService.ChannelPointsRedeem += OnRedeemTriggered;
        twitchService.ChannelChatMessage += OnMessageTriggered;
        twitchService.ChannelRaid += OnRaidTriggered;

        // TODO: This needs to be less hardcoded
        // worldRoot = GetNode<Node3D>("../World");
        // avatarAnchor = GetNode<Node3D>("../World/AvatarAnchor");
    }

    public static void RegisterEventDomain(IEventDomain newDomain)
    {
        if (!_instance.eventDomains.Contains(newDomain))
        {
            _instance.eventDomains.Add(newDomain);
        }
    }

    public static void SendEventLookup(BeepoEventLookup eventLookup)
    {
        foreach (IEventDomain d in _instance.eventDomains)
        {
            if (d.EventDomainID == eventLookup.eventDomainID)
            {
                d.TriggerEvent(eventLookup);
            }
        }
    }

    private void OnRedeemTriggered(TwitchRedeemPayload payload)
    {
        DebugLog(string.Format("Redeem: {0}, Displayname: {1}, Input: {2}", payload.data.reward.title, payload.data.user_name, payload.data.user_input));

        if (channelRedeems.TryGetValue(payload.data.reward.title, out ChannelRedeemNode redeemNode))
        {
            redeemNode.ExecuteChannelRedeem(payload);
        }
    }

    private void OnRaidTriggered(TwitchRaidPayload payload)
    {
        DebugLog(string.Format("RaiderUsername: {0}, RaiderDisplayName: {1}, RaidCount: {2}", payload.data.from_broadcaster_user_login, payload.data.from_broadcaster_user_name, payload.data.viewers));

        foreach (RaidEventNode r in raidEvents)
        {
            r.ExecuteRaidEvent(payload);
        }
    }

    private void OnMessageTriggered(TwitchChatMessagePayload payload)
    {
        DebugLog(string.Format("FromUsername: {0}, FromDisplayName: {1}, Message: {2}, Bits: {3}", payload.data.chatter_user_login, payload.data.chatter_user_name, payload.data.message.text, payload.data.cheer.bits));

        string messageText = payload.data.message.text;
        if (messageText[0] == commandPrefix)
        {
            if (messageText.Length > 1)
            {
                string[] commandString = messageText.Substring(1).Split(' ');
                if (chatCommands.TryGetValue(commandString[0], out ChatCommandNode chatCommand))
                {
                    string[] commandParams;
                    if (commandString.Length > 1)
                    {
                        commandParams = new string[commandString.Length - 1];
                        Array.Copy(commandString, 1, commandParams, 0, commandParams.Length);
                    }
                    else
                    {
                        commandParams = new string[0];
                    }
                    chatCommand.ExecuteCommand(payload, commandParams);
                }
            }
        }
    }

    public void RegisterChatCommand(ChatCommandNode toAdd)
    {
        if (!chatCommands.ContainsKey(toAdd.CommandName))
        {
            chatCommands.Add(toAdd.CommandName, toAdd);
        }
    }

    public void UnregisterChatCommand(ChatCommandNode toRemove)
    {
        chatCommands.Remove(toRemove.CommandName);
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
        if (!channelRedeems.ContainsKey(toAdd.RedeemTitle))
        {
            channelRedeems.Add(toAdd.RedeemTitle, toAdd);
        }
    }

    public void UnregisterChannelRedeem(ChannelRedeemNode toRemove)
    {
        channelRedeems.Remove(toRemove.RedeemTitle);
    }

    public bool HasBadge(int badges, TwitchBadge badge)
    {
        return (badges & (int)badge) != 0;
    }

    public static void SendTwitchMessage(string newMessage)
    {
        _instance.twitchService.twitchServiceIRC.ClientIRCSend("PRIVMSG #" + _instance.twitchService.twitchUsername + " :" + newMessage);
    }

    public static void RegisterNewAvatar(BeepoAvatar newAvatar)
    {
        if (currentAvatars.Contains(newAvatar))
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

    public static void DebugLog(string debugText)
    {
        GD.Print(Time.GetTimeStringFromSystem() + " : " + debugText);
        _instance.EmitSignal(BeepoCore.SignalName.OnDebugLog, Time.GetTimeStringFromSystem() + " : " + debugText);
    }
}
