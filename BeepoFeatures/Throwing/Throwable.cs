using Godot;
using Godot.Collections;
using System;

/// <summary>
/// A throwable object. Can be added to a ThrowPath for guided movement.
/// </summary>
public partial class Throwable : RigidBody3D
{
    [Export] public Array<BeepoEventLookup> onHitEvents = new Array<BeepoEventLookup>();

    private Tween tweenPath;
    private bool firstCollision = false;
    public bool FirstCollision { get { return firstCollision; } }

    [Export] private CollisionShape3D collider;
    [Export] private bool initialImpactEffectsOnly = true;
    [Export] private AudioStreamPlayer3D throwAudio;
    [Export] private PackedScene[] impactVFX;

    private float lifeTime = 20.0f;

    public override void _Ready()
    {
        Connect(SignalName.BodyEntered, Callable.From((Node body) => { OnBodyEnter(body); }));
    }

    public override void _Process(double delta)
    {
        if (lifeTime <= 0)
        {
            QueueFree();
            return;
        }
        lifeTime -= (float)delta;
    }

    public override void _IntegrateForces(PhysicsDirectBodyState3D state)
    {
        if (state.GetContactCount() > 0)
        {
            if (!firstCollision)
            {
                firstCollision = true;
                DetachFromTweenPath();

                if (!onHitEvents.IsNullOrEmpty())
                {
                    foreach (BeepoEventLookup e in onHitEvents)
                    {
                        BeepoCore.SendEventLookup(e);
                    }
                }

                throwAudio.Play();

                //     if (!impactVFX.IsNullOrEmpty())
                //     {
                //         PackedScene vfxScene = impactVFX.GetRandom();
                //         VFXPlayer vfx = vfxScene.Instantiate<VFXPlayer>();
                //         BeepoCore.WorldRoot.AddChild(vfx);
                //         vfx.GlobalPosition = state.GetContactColliderPosition(0);
                //     }
            }
            else if (!initialImpactEffectsOnly)
            {
                throwAudio.Play();

                // if (!impactVFX.IsNullOrEmpty())
                // {
                //     PackedScene vfxScene = impactVFX.GetRandom();
                //     VFXPlayer vfx = vfxScene.Instantiate<VFXPlayer>();
                //     BeepoCore.WorldRoot.AddChild(vfx);
                //     vfx.GlobalPosition = state.GetContactColliderPosition(0);
                // }
            }
        }
    }

    private void OnBodyEnter(Node other)
    {
        // if (!firstCollision)
        // {
        //     firstCollision = true;
        //     DetachFromTweenPath();
        //     throwAudio.Play();

        //     if(onHitEventID != null)
        //     {
        //         //TODO: Call event from BeepoCore
        //     }
        // }
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
