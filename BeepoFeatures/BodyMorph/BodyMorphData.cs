using Godot;
using System;

[GlobalClass]
public partial class BodyMorphData : Node
{
    [Export] public BodyMorpher targetMorpher;
    [Export] public Vector3 morphTo;
}
