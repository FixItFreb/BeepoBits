using System;
using Godot;

public partial class EventDomainNode<T> : SingletonNode<T>, IEventDomain where T : EventDomainNode<T>
{
    [Export] public StringName EventDomainID { get; private set; }

    public virtual void TriggerEvent(BeepoEventLookup eventLookup)
    {
    }

    public override void _EnterTree()
    {
        base._EnterTree();
        
        if (!IsQueuedForDeletion())
        {
            BeepoCore.RegisterEventDomain(this);
        }
    }
}