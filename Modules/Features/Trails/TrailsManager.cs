using Godot;
using Godot.Collections;
using System;

public partial class TrailsManager : EventDomainNode<TrailsManager>, IEventDomain
{
    [Export] public Array<TrailSet> trailSets = new Array<TrailSet>();

    public void TriggerSet(StringName setName, float duration = 0)
    {
        foreach(TrailSet t in trailSets)
        {
            if(t.setName == setName)
            {
                if (duration <= 0)
                {
                    t.Toggle();
                }
                else
                {
                    t.Trigger(duration);
                }
            }
        }
    }
}
