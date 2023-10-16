using Godot;
using Godot.Collections;
using System;

public partial class RandomColor : Node
{
    [Export] private Color[] colorSelection;
    [Export] private MeshInstance3D mesh;

    public override void _Ready()
    {
        if (!colorSelection.IsNullOrEmpty())
        {
            mesh.MaterialOverride.Set("albedo_color", colorSelection.GetRandom());
        }
    }
}
