using Godot;
using System;

public partial class BeepoAudio : Node
{
    private static BeepoAudio _instance;
    public static BeepoAudio Instance { get { return _instance; } }

    [Export] private AudioStreamPlayer audioPlayer;

    public override void _EnterTree()
    {
        _instance = this;
    }

    public static void PlayAudio(AudioStream toPlay)
    {
        _instance.audioPlayer.PlayAudio(toPlay);
    }
}
