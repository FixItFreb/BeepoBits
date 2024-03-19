using Godot;
using System;

public partial class WingMovement : Node3D
{
	[Export] private PNGBone frontWing;
	[Export] private PNGBone backWing;

	private Vector3 initialFrontRotation;
	private Vector3 initialBackRotation;

	private double frontRotationProgress = 0;
	private double backRotationProgress = 0;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		initialFrontRotation = frontWing.Rotation;
		initialBackRotation = backWing.Rotation;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		frontRotationProgress += delta;
		backRotationProgress += delta;

		var frontRotationOffset = Math.Sin(frontRotationProgress * 5) * 0.5;
		var backRotationOffset = Math.Sin((backRotationProgress + 0.1) * 5) * 0.5;

		var frontFinalRotation = initialFrontRotation;
		var backFinalRotation = initialBackRotation;

		frontFinalRotation.Z += (float)frontRotationOffset;
		backFinalRotation.Z += (float)backRotationOffset;

		frontWing.Rotation = frontFinalRotation;
		backWing.Rotation = backFinalRotation;
	}
}
