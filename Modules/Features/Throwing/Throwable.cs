using Godot;
using System;

/// <summary>
/// A throwable object. Can be added to a ThrowPath for guided movement.
/// </summary>
public partial class Throwable : RigidBody3D
{
    private Tween tweenPath;
    private bool firstCollision = false;
    public bool FirstCollision { get { return firstCollision; } }

    [Export] private CollisionShape3D collider;
    [Export] private AudioStreamPlayer3D throwAudio;

    public override void _Ready()
    {
        Connect(SignalName.BodyEntered, Callable.From((Node body) => { OnBodyEnter(body); }));
    }

    private void OnBodyEnter(Node other)
    {
        if (!firstCollision)
        {
            firstCollision = true;
            DetachFromTweenPath();
            throwAudio.Play();
        }
    }

    public void SetTweenPath(Tween newTweenPath)
    {
        tweenPath = newTweenPath;
        Freeze = true;
    }

    public void DetachFromTweenPath()
    {
        if (tweenPath != null)
        {
            tweenPath.Kill();
            tweenPath = null;
        }
        Freeze = false;
    }
}
