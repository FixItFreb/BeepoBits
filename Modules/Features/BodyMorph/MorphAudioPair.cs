using Godot;

[GlobalClass]
public partial class MorphAudioPair : Resource
{
    [Export] public AudioStream morphUpAudio;
    [Export] public AudioStream morphDownAudio;
}