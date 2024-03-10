using Godot;
using System;

[Tool]
public partial class ThrowPath : Path3D
{
    [Export] private bool runInEditor = false;
    [Export] private PathFollow3D pathFollower;
    [Export] private Node3D fromNode;
    [Export] private Node3D targetNode;
    [Export] private AnimationPlayer animator;
    [Export] private Throwable attachedThrowable;

    public override void _Process(double delta)
    {
        if(Engine.IsEditorHint() && runInEditor)
        {
            runInEditor = false;
            SetPath(fromNode, targetNode);
        }
    }

    public void SetPath(Node3D start, Node3D end)
    {
        float throwPathY = start.GlobalPosition.DistanceTo(end.GlobalPosition) * 0.1f;
        Curve.SetPointPosition(0, Vector3.Zero.WithY(end.GlobalPosition.Y));

        Vector3 endPoint = end.GlobalPosition - start.GlobalPosition;
        Curve.SetPointPosition(1, endPoint);

        Curve.SetPointOut(0, new Vector3(endPoint.X * 0.5f, throwPathY, endPoint.Z * 0.5f));
        Curve.SetPointIn(1, new Vector3(-endPoint.X * 0.5f, throwPathY, -endPoint.Z * 0.5f));

        animator.Play("Throw");
    }

    public void ThrowComplete()
    {
        attachedThrowable.DetachFromTweenPath();
    }
}
