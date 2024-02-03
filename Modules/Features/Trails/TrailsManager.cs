using Godot;
using Godot.Collections;
using System;

public partial class TrailsManager : Node
{
    private static TrailsManager _instance;
    public static TrailsManager Instance { get { return _instance; } }

    [Export] public Array<TrailSet> trailSets = new Array<TrailSet>();

    public override void _EnterTree()
    {
        _instance = this;
    }

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
