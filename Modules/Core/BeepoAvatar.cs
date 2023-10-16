using Godot;
using System;

public partial class BeepoAvatar : Node3D
{
    [Export] public Node3D headPostion;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
	{
        BeepoCore.currentAvatars.Add(this);
    }

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

    public override void _EnterTree()
    {
        BeepoCore.currentAvatars.Add(this);
    }

    public override void _ExitTree()
    {
        BeepoCore.currentAvatars.Remove(this);
    }
}
