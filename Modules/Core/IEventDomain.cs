using System;
using Godot;

public interface IEventDomain
{
    StringName EventDomainID { get; }
    void TriggerEvent(BeepoEventLookup eventLookup);
}