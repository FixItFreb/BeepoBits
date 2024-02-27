using Godot;
using System;

public partial class BodyMorphManager : EventDomainNode<BodyMorphManager>, IEventDomain
{
    [Export] public BodyMorphSet[] morphSets;

    public void TriggerSet(StringName bodyMorphSet)
    {
        foreach(BodyMorphSet b in morphSets)
        {
            if(b.setName == bodyMorphSet)
            {
                b.ApplyMorphSet();
            }
        }
    }
}
