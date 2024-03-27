using Godot;
using GDC = Godot.Collections;
using System;
using System.Collections.Generic;

public partial class BeepoCore : Node
{
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

    public Camera3D CurrentCamera { get { return GetViewport().GetCamera3D(); } }

    public override void _EnterTree()
    {
    }

    public override void _Ready()
    {

        // TODO: This needs to be less hardcoded
        // worldRoot = GetNode<Node3D>("../World");
        // avatarAnchor = GetNode<Node3D>("../World/AvatarAnchor");

    }

    public static BeepoCore GetInstance()
    {
        return ((SceneTree)Engine.GetMainLoop()).Root.GetNode<BeepoCore>("BeepoBits_Core");
    }

    public bool RegisterEventDomain(EventDomainNode newDomain)
    {
        GD.Print("Registered event domain ", newDomain.EventDomainID);
        if (!eventDomains.ContainsKey(newDomain.EventDomainID))
        {
            eventDomains.Add(newDomain.EventDomainID, newDomain);
            return true;
        }
        return false;
    }

    public bool RegisterEventListener(StringName eventDomainID, IBeepoListener newListener)
    {
        GD.Print("Registered new listener in event domain ", eventDomainID);
        EventDomainNode eventDomain;
        if (!eventDomains.TryGetValue(eventDomainID, out eventDomain))
        {
            return false;
        }

        eventDomain.AddListener(newListener);
        return true;
    }

    public void SendEventLookup(BeepoEvent beepoEvent)
    {
        GD.Print("Sending event in event domain ", beepoEvent.EventDomainID);
        EventDomainNode eventDomain;
        if (!eventDomains.TryGetValue(beepoEvent.EventDomainID, out eventDomain))
        {
            GD.PrintErr("Tried sending event to domain ", beepoEvent.EventDomainID, " but no such domain exists.");
            return;
        }

        eventDomain.NotifyListeners(beepoEvent);

    }

    public void RegisterNewAvatar(BeepoAvatar newAvatar)
    {
        if (currentAvatars.Contains(newAvatar))
        {
            return;
        }

        currentAvatars.Add(newAvatar);
        EmitSignal(BeepoCore.SignalName.NewAvatarRegistered, newAvatar);
    }

    public void UnregisterAvatar(BeepoAvatar avatar)
    {
        if (currentAvatars.Contains(avatar))
        {
            currentAvatars.Remove(avatar);
            EmitSignal(BeepoCore.SignalName.AvatarUnregistered, avatar);
        }
    }

    public void DebugLog(string debugText)
    {
        GD.Print(Time.GetTimeStringFromSystem() + " : " + debugText);
        EmitSignal(BeepoCore.SignalName.OnDebugLog, Time.GetTimeStringFromSystem() + " : " + debugText);
    }
}
