using Godot;
using System;
using System.Linq;

public enum SpeechState
{
	Always,
	NotSpeaking,
	Speaking,
}

[GlobalClass]
public partial class PNGSpriteSegment : Sprite3D
{
	// Sprite display control
	[Export] private SpeechState speech;
	[Export] private Shape3D collisionShape;

	public void SetSpeech(SpeechState newSpeech)
	{
		Visible = speech == SpeechState.Always || speech == newSpeech;
	}

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}
