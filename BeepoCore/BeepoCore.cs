using Godot;
using GDC = Godot.Collections;
using System;
using System.Collections.Generic;

public partial class BeepoCore : Node
{
    [Export] public char commandPrefix = '!';

    public GDC.Dictionary<StringName, EventDomainNode> eventDomains = new();
    [Signal] public delegate void OnDebugLogEventHandler(string debugString);

    public Camera3D CurrentCamera { get { return GetViewport().GetCamera3D(); } }

    public override void _EnterTree()
    {
    }

    public override void _Ready()
    {

    }

    public static BeepoCore GetInstance()
    {
        return ((SceneTree)Engine.GetMainLoop()).Root.GetNode<BeepoCore>("BeepoBits_Core");
    }

    public bool RegisterEventDomain(EventDomainNode newDomain)
    {
        // GD.Print("Registered event domain ", newDomain.EventDomainID);
        if (!eventDomains.ContainsKey(newDomain.EventDomainID))
        {
            eventDomains.Add(newDomain.EventDomainID, newDomain);
            return true;
        }
        return false;
    }

    public bool RegisterEventListener(StringName eventDomainID, IBeepoListener newListener)
    {
        // GD.Print("Registered new listener in event domain ", eventDomainID);
        EventDomainNode eventDomain;
        if (!eventDomains.TryGetValue(eventDomainID, out eventDomain))
        {
            return false;
        }

        eventDomain.AddListener(newListener);
        return true;
    }

    public void SendEvent(BeepoEvent beepoEvent, StringName eventDomainID)
    {
        // GD.Print("Sending event in event domain ", beepoEvent.EventDomainID);
        EventDomainNode eventDomain;
        if (!eventDomains.TryGetValue(eventDomainID, out eventDomain))
        {
            GD.PrintErr("Tried sending event to domain ", eventDomainID, " but no such domain exists.");
            return;
        }

        eventDomain.NotifyListeners(beepoEvent);

    }

    public void DebugLog(string debugText)
    {
        GD.Print(Time.GetTimeStringFromSystem() + " : " + debugText);
        EmitSignal(BeepoCore.SignalName.OnDebugLog, Time.GetTimeStringFromSystem() + " : " + debugText);
    }
}
