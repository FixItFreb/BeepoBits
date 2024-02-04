using Godot;
using System;

// Just Stub for now, need to wait for 4.3
[GlobalClass]
[Tool]
public partial class PNGCollision : Node3D
{
	[Export] private Shape3D shape;
	public Shape3D Shape { get { return shape; } }

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}
