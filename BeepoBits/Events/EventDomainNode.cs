using System;
using System.Collections.Generic;
using Godot;

[GlobalClass]
public partial class EventDomainNode : Node
{
    [Export] public StringName EventDomainID { get; private set; }
    List<IBeepoListener> listeners = new();

    public void AddListener(IBeepoListener listener)
    {
        listeners.Add(listener);
    }

    public void NotifyListeners(BeepoEvent beepoEvent)
    {
        foreach (var listener in listeners)
        {
            listener.Notify(beepoEvent);
        }
    }

    public override void _EnterTree()
    {
        var beepoCore = BeepoCore.GetInstance();
        if (!beepoCore.RegisterEventDomain(this)) GD.PrintErr("Failed to register domain {0}: name already in use", EventDomainID);
    }
}