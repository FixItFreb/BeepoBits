using Godot;
using System;

public partial class AvatarSprite : Sprite3D
{

	// Sprite changing
	private Texture2D silentTexture; // silent texture is just the configured Sprite3D texture
	[Export] private Texture2D speakingTexture;

	public void StartSpeaking()
	{
		Texture = speakingTexture;
	}

	public void StopSpeaking()
	{
		Texture = silentTexture;
	}

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		silentTexture = Texture;
	}
}
