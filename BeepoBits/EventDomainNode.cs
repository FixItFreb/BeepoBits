using System;
using System.Collections.Generic;
using Godot;

[GlobalClass]
public partial class EventDomainNode : Node
{
    [Export] public StringName EventDomainID { get; private set; }
    List<ListenerNode> listeners = new();

    public void AddListener(ListenerNode node)
    {
        listeners.Add(node);
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
        var beepoCore = GetNode<BeepoCore>("/root/BeepoBits_Core");
        if (!beepoCore.RegisterEventDomain(this)) GD.PrintErr("Failed to register domain {0}: name already in use", EventDomainID);
    }
}