using Godot;
using Godot.Collections;
using System;

[GlobalClass]
public partial class BodyMorphSet : Node
{
    [Export] public StringName setName;
    [Export] public float morphDuration;
    [Export] public float morphToTime = 1.0f;
    [Export] public float morphBackTime = 1.0f;
    [Export] public BodyMorphData[] morphData;
    [Export] public MorphAudioPair[] morphAudio;

    public void ApplyMorphSet()
    {
        for (int i = 0; i < morphData.Length; i++)
        {
            morphData[i].targetMorpher.ApplyMorph(this, i);
        }
    }
}