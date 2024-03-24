using Godot;
using GDC = Godot.Collections;
using System;
using System.Collections.Generic;

public partial class BeepoCore : Node
{
    private BeepoCore _instance;
    public BeepoCore Instance { get { return _instance; } }

    [Export] public char commandPrefix = '!';

    public static List<BeepoAvatar> currentAvatars = new List<BeepoAvatar>();

    private static Node3D avatarAnchor;
    public static Node3D AvatarAnchor { get { return avatarAnchor; } }
    private static Node3D worldRoot;
    public static Node3D WorldRoot { get { return worldRoot; } }

    public GDC.Dictionary<StringName, EventDomainNode> eventDomains = new();

    [Signal] public delegate void NewAvatarRegisteredEventHandler(BeepoAvatar newAvatar);
    [Signal] public delegate void AvatarUnregisteredEventHandler(BeepoAvatar avatar);
    [Signal] public delegate void OnDebugLogEventHandler(string debugString);

    public Camera3D CurrentCamera { get { return _instance.GetViewport().GetCamera3D(); } }

    public override void _EnterTree()
    {
        _instance = this;
    }

    public override void _Ready()
    {

        // TODO: This needs to be less hardcoded
        // worldRoot = GetNode<Node3D>("../World");
        // avatarAnchor = GetNode<Node3D>("../World/AvatarAnchor");

    }

    public bool RegisterEventDomain(EventDomainNode newDomain)
    {
        if (!eventDomains.ContainsKey(newDomain.EventDomainID))
        {
            eventDomains.Add(newDomain.EventDomainID, newDomain);
            return true;
        }
        return false;
    }

    public void SendEventLookup(BeepoEvent beepoEvent)
    {
        EventDomainNode eventDomain;
        if (!eventDomains.TryGetValue(beepoEvent.eventDomainID, out eventDomain))
        {
            GD.PrintErr("Tried sending event to domain {0} but no such domain exists.");
            return;
        }

        eventDomain.NotifyListeners(beepoEvent);

    }

    // private void OnRedeemTriggered(TwitchRedeemPayload payload)
    // {
    //     DebugLog(string.Format("Redeem: {0}, Displayname: {1}, Input: {2}", payload.data.reward.title, payload.data.user_name, payload.data.user_input));

    //     if (channelRedeems.TryGetValue(payload.data.reward.title, out ChannelRedeemNode redeemNode))
    //     {
    //         redeemNode.ExecuteChannelRedeem(payload);
    //     }
    // }

    // private void OnRaidTriggered(TwitchRaidPayload payload)
    // {
    //     DebugLog(string.Format("RaiderUsername: {0}, RaiderDisplayName: {1}, RaidCount: {2}", payload.data.from_broadcaster_user_login, payload.data.from_broadcaster_user_name, payload.data.viewers));

    //     foreach (RaidEventNode r in raidEvents)
    //     {
    //         r.ExecuteRaidEvent(payload);
    //     }
    // }

    // private void OnMessageTriggered(TwitchChatMessagePayload payload)
    // {
    //     DebugLog(string.Format("FromUsername: {0}, FromDisplayName: {1}, Message: {2}, Bits: {3}", payload.data.chatter_user_login, payload.data.chatter_user_name, payload.data.message.text, payload.data.cheer.bits));

    //     string messageText = payload.data.message.text;
    //     if (messageText[0] == commandPrefix)
    //     {
    //         if (messageText.Length > 1)
    //         {
    //             string[] commandString = messageText.Substring(1).Split(' ');
    //             if (chatCommands.TryGetValue(commandString[0], out ChatCommandNode chatCommand))
    //             {
    //                 string[] commandParams;
    //                 if (commandString.Length > 1)
    //                 {
    //                     commandParams = new string[commandString.Length - 1];
    //                     Array.Copy(commandString, 1, commandParams, 0, commandParams.Length);
    //                 }
    //                 else
    //                 {
    //                     commandParams = new string[0];
    //                 }
    //                 chatCommand.ExecuteCommand(payload, commandParams);
    //             }
    //         }
    //     }
    // }

    public bool HasBadge(int badges, TwitchBadge badge)
    {
        return (badges & (int)badge) != 0;
    }

    // public void SendTwitchMessage(string newMessage)
    // {
    //     _instance.twitchService.twitchServiceIRC.ClientIRCSend("PRIVMSG #" + _instance.twitchService.twitchUsername + " :" + newMessage);
    // }

    public void RegisterNewAvatar(BeepoAvatar newAvatar)
    {
        if (currentAvatars.Contains(newAvatar))
        {
            return;
        }

        currentAvatars.Add(newAvatar);
        _instance.EmitSignal(BeepoCore.SignalName.NewAvatarRegistered, newAvatar);
    }

    public void UnregisterAvatar(BeepoAvatar avatar)
    {
        if (currentAvatars.Contains(avatar))
        {
            currentAvatars.Remove(avatar);
            _instance.EmitSignal(BeepoCore.SignalName.AvatarUnregistered, avatar);
        }
    }

    public void DebugLog(string debugText)
    {
        GD.Print(Time.GetTimeStringFromSystem() + " : " + debugText);
        _instance.EmitSignal(BeepoCore.SignalName.OnDebugLog, Time.GetTimeStringFromSystem() + " : " + debugText);
    }
}
