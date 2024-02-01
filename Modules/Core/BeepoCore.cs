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

    [Signal] public delegate void NewAvatarRegisteredEventHandler(BeepoAvatar newAvatar);
    [Signal] public delegate void AvatarUnregisteredEventHandler(BeepoAvatar avatar);
    [Signal] public delegate void OnDebugLogEventHandler(string debugString);

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

    private void OnRedeemTriggered(TwitchRedeemPayload payload)
    {
        DebugLog(string.Format("Redeem: {0}, Displayname: {1}, Input: {2}", payload.data.reward.title, payload.data.user_name, payload.data.user_input));
    }

    private void OnRaidTriggered(TwitchRaidPayload payload)
    {
        DebugLog(string.Format("RaiderUsername: {0}, RaiderDisplayName: {1}, RaidCount: {2}", payload.data.from_broadcaster_user_login, payload.data.from_broadcaster_user_name, payload.data.viewers));
    }

    private void OnMessageTriggered(TwitchChatMessagePayload payload)
    {
        DebugLog(string.Format("FromUsername: {0}, FromDisplayName: {1}, Message: {2}, Bits: {3}", payload.data.chatter_user_login, payload.data.chatter_user_name, payload.data.message.text, payload.data.cheer.bits));

        // if(message[0] == commandPrefix)
        // {
        //     string[] splitMessage = message.Substring(1).Split(' ');
        //     if (splitMessage.Length > 0)
        //     {
        //         string commandString = splitMessage[0];
        //         if(chatCommands.TryGetValue(commandString, out ChatCommandNode chatCommand))
        //         {
        //             if ((int)chatCommand.requiredBadges == 0 || (badges & (int)chatCommand.requiredBadges) != 0)
        //             {
        //                 chatCommand.ExecuteCommand(new ChatCommandPayload(fromUsername, fromDisplayName, message, bits, badges));
        //             }
        //         }
        //     }
        // }
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
        if(!channelRedeems.ContainsKey(toAdd.RedeemTitle))
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

    public static void DebugLog(string debugText)
    {
        GD.Print(Time.GetTimeStringFromSystem() + " : " + debugText);
        _instance.EmitSignal(BeepoCore.SignalName.OnDebugLog, Time.GetTimeStringFromSystem() + " : " + debugText);
    }
}
