using Godot;
using System;

public partial class BodyMorphManager : Node
{
    private static BodyMorphManager _instance;
    public static BodyMorphManager Instance { get { return _instance; } }
    
    [Export] public BodyMorphSet[] morphSets;

    public override void _EnterTree()
    {
        _instance = this;
    }

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
