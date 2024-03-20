using Godot;
using Godot.Collections;
using System;
using System.Linq;
using System.Collections.Generic;

[GlobalClass]
public partial class PNGBone : Node3D
{
	Array<PNGBone> subbones;
	Array<PNGSpriteSegment> sprites;


	public void UpdateSpeech(SpeechState speech)
	{
		foreach (PNGSpriteSegment sprite in sprites)
		{
			sprite.SetSpeech(speech);
		}

		foreach (PNGBone bone in subbones)
		{
			bone.UpdateSpeech(speech);
		}
	}
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		Array<Node> children = GetChildren();

		IEnumerable<Node> foundSubbones = children.Where(child => child is PNGBone);
		IEnumerable<Node> foundSprites = children.Where(child => child is PNGSpriteSegment);

		subbones = new Array<PNGBone>(foundSubbones.Cast<PNGBone>());
		sprites = new Array<PNGSpriteSegment>(foundSprites.Cast<PNGSpriteSegment>());
	}
}
