using Godot;
using Godot.Collections;
using System;

public partial class TrailPoint : GodotObject
{
    public Vector3 position;
    public Vector3 widthFrom;
    public Vector3 widthTo;
    public float lifetime;

    public TrailPoint(Vector3 _position, Vector3 _widthFrom, Vector3 _widthTo, float _lifetime)
    {
        position = _position;
        widthFrom = _widthFrom;
        widthTo = _widthTo;
        lifetime = _lifetime;
    }
}

/// <summary>
/// Draw a trail mesh from the attached node
/// Based on tutorial found here: https://www.youtube.com/watch?v=vKrrxKS-lcA
/// </summary>
public partial class TrailEffect : MeshInstance3D
{
    private Array<TrailPoint> trailPoints = new Array<TrailPoint>();

    [Export] public Node3D trailAnchor;

    // Is this trail drawn?
    private bool enabled = false;
    [Export]
    public bool Enabled
    {
        get { return enabled; }
        set
        {
            enabled = value;
        }
    }

    [Export] public float fromWidth = 0.5f;
    [Export] public float toWidth = 0.0f;
    [Export(PropertyHint.Range, "0.5f, 1.5f")] public float scaleSpeed = 1.0f;
    [Export] public bool scaleTexture = true;
    [Export] public float motionDelta = 0.1f;
    [Export] public float duration = 1.0f;

    [Export] public Gradient trailGradient;

    private Vector3 prevPos;
    private float lifetime = 0.0f;
    private ImmediateMesh trailMesh;

    public override void _Ready()
    {
        prevPos = trailAnchor.GlobalPosition;
        Mesh = new ImmediateMesh();
        trailMesh = (ImmediateMesh)Mesh;
    }

    private void AddPoint()
    {
        trailPoints.Add(new TrailPoint(trailAnchor.GlobalPosition,
        trailAnchor.GlobalTransform.Basis.X * fromWidth,
        trailAnchor.GlobalTransform.Basis.X * fromWidth - trailAnchor.GlobalTransform.Basis.X * toWidth,
        0.0f));
    }

    private void RemovePoint(int index)
    {
        trailPoints.RemoveAt(index);
        trailMesh.ClearSurfaces();
    }

    private void Clear()
    {
        trailPoints.Clear();
        trailMesh.ClearSurfaces();
    }

    public override void _PhysicsProcess(double delta)
    {
        if(!enabled && trailPoints.Count == 0)
        {
            return;
        }

        if (enabled &&(prevPos - trailAnchor.GlobalPosition).Length() > motionDelta)
        {
            trailPoints.Add(new TrailPoint(
                trailAnchor.GlobalPosition,
                trailAnchor.GlobalTransform.Basis.X * fromWidth,
                trailAnchor.GlobalTransform.Basis.X * fromWidth - trailAnchor.GlobalTransform.Basis.X * toWidth,
                0.0f));
            prevPos = trailAnchor.GlobalPosition;
        }

        int pointIndex = 0;
        int pointCount = trailPoints.Count;
        while (pointIndex < pointCount)
        {
            trailPoints[pointIndex].lifetime += (float)delta;
            if (trailPoints[pointIndex].lifetime > duration)
            {
                trailPoints.RemoveAt(pointIndex);
                pointIndex = Mathf.Max(0, pointIndex - 1);
            }
            pointCount = trailPoints.Count;
            pointIndex++;
        }

        trailMesh.ClearSurfaces();

        if (trailPoints.Count >= 2)
        {
            trailMesh.SurfaceBegin(Mesh.PrimitiveType.TriangleStrip);

            for (int i = 0; i < trailPoints.Count; i++)
            {
                TrailPoint point = trailPoints[i];
                float t = (float)i / (trailPoints.Count - 1.0f);
                Vector3 currentWidth = point.widthFrom - Mathf.Pow(1 - t, scaleSpeed) * point.widthTo;

                float t0;
                float t1;

                if (scaleTexture)
                {
                    t0 = motionDelta * i;
                    t1 = motionDelta * (i + 1);
                }
                else
                {
                    t0 = 1.0f / trailPoints.Count;
                    t1 = t;
                }

                trailMesh.SurfaceSetColor(trailGradient.Sample(1f - t));
                trailMesh.SurfaceSetUV(new Vector2(t0, 0));
                trailMesh.SurfaceAddVertex(ToLocal(point.position + currentWidth));
                trailMesh.SurfaceSetNormal(Vector3.Up);
                trailMesh.SurfaceSetUV(new Vector2(t1, 1));
                trailMesh.SurfaceAddVertex(ToLocal(point.position - currentWidth));
            }
            trailMesh.SurfaceEnd();
        }
    }
}
