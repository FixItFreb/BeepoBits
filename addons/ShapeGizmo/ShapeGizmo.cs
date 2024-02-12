using System;
using System.Collections.Generic;
using Godot;

public partial class ShapeGizmo : EditorNode3DGizmoPlugin
{
  private EditorUndoRedoManager undoRedo;
  private GizmoHelper helper;

  public ShapeGizmo(EditorUndoRedoManager manager)
  {
    undoRedo = manager;
    helper = new(manager);
  }

  public override string _GetGizmoName()
  {
    return "PNGCollisionShape";
  }

  public override bool _HasGizmo(Node3D forNode3D)
  {
    return forNode3D is PNGCollision;
  }

  public override string _GetHandleName(EditorNode3DGizmo gizmo, int handleId, bool secondary)
  {
    PNGCollision node = (PNGCollision)gizmo.GetNode3D();
    Shape3D shape = node.Shape;

    if (shape is SphereShape3D)
    {
      return "Radius";
    }

    if (shape is BoxShape3D)
    {
      return GizmoHelper.BoxGetHandleName(handleId);
    }

    if (shape is CapsuleShape3D)
    {
      return handleId == 0 ? "Radius" : "Height";
    }

    if (shape is CylinderShape3D)
    {
      return handleId == 0 ? "Radius" : "Height";
    }

    if (shape is SeparationRayShape3D)
    {
      return "Length";
    }

    return "";
  }

  public override Variant _GetHandleValue(EditorNode3DGizmo gizmo, int handleId, bool secondary)
  {
    PNGCollision node = (PNGCollision)gizmo.GetNode3D();
    Shape3D shape = node.Shape;

    if (shape is SphereShape3D sphere)
    {
      return sphere.Radius;
    }

    if (shape is BoxShape3D box)
    {
      return box.Size;
    }

    if (shape is CapsuleShape3D capsule)
    {
      return new Vector2(capsule.Radius, capsule.Height);
    }

    if (shape is CylinderShape3D cylinder)
    {
      return handleId == 0 ? cylinder.Radius : cylinder.Height;
    }

    if (shape is SeparationRayShape3D separationRay)
    {
      return separationRay.Length;
    }

    return new();
  }

  public override void _SetHandle(EditorNode3DGizmo gizmo, int handleId, bool secondary, Camera3D camera, Vector2 screenPos)
  {
    PNGCollision node = (PNGCollision)gizmo.GetNode3D();
    Shape3D shape = node.Shape;
    if (shape == null) return;
    var segment = helper.GetSegment(camera, screenPos);

    if (shape is SphereShape3D sphere)
    {
      var points = Geometry3D.GetClosestPointsBetweenSegments(new Vector3(), new Vector3(4096, 0, 0), segment[0], segment[1]);
      var pointA = points[0];
      float distance = pointA.X;

      if (distance < 0.001f) distance = 0.001f;
      sphere.Radius = distance;
    }

    if (shape is BoxShape3D box)
    {
      var size = box.Size;
      var position = new Vector3();
      helper.BoxSetHandle(segment, handleId, out size, out position);
      box.Size = size;
      node.Position = position;
    }

    if (shape is CapsuleShape3D capsule)
    {
      var axis = new Vector3();
      axis[handleId == 0 ? 0 : 1] = 1.0f;
      var points = Geometry3D.GetClosestPointsBetweenSegments(new Vector3(), axis * 4096, segment[0], segment[1]);
      var pointA = points[0];
      var distance = axis.Dot(pointA);

      if (distance < 0.001f) distance = 0.001f;
      if (handleId == 0)
      {
        capsule.Radius = distance;
      }
      else if (handleId == 1)
      {
        capsule.Height = distance * 2.0f;
      }
    }

    if (shape is CylinderShape3D cylinder)
    {
      var axis = new Vector3();
      axis[handleId == 0 ? 0 : 1] = 1.0f;
      var points = Geometry3D.GetClosestPointsBetweenSegments(new Vector3(), axis * 4096, segment[0], segment[1]);
      var pointA = points[0];
      var distance = axis.Dot(pointA);

      if (distance < 0.001f) distance = 0.001f;
      if (handleId == 0)
      {
        cylinder.Radius = distance;
      }
      else if (handleId == 1)
      {
        cylinder.Height = distance * 2.0f;
      }
    }

    if (shape is SeparationRayShape3D separationRay)
    {
      var points = Geometry3D.GetClosestPointsBetweenSegments(new Vector3(), new Vector3(0, 0, 4096), segment[0], segment[1]);
      var pointA = points[0];
      float distance = pointA.Z;

      if (distance < 0.001f) distance = 0.001f;
      separationRay.Length = distance;
    }
  }

  public override void _CommitHandle(EditorNode3DGizmo gizmo, int handleId, bool secondary, Variant restore, bool cancel)
  {
    PNGCollision node = (PNGCollision)gizmo.GetNode3D();
    Shape3D shape = node.Shape;
    if (shape == null) return;

    if (shape is SphereShape3D sphere)
    {
      if (cancel)
      {
        sphere.Radius = (float)restore;
        return;
      }

      undoRedo.CreateAction("Change Sphere Shape Radius");
      undoRedo.AddDoMethod(sphere, "set_radius", sphere.Radius);
      undoRedo.AddUndoMethod(sphere, "set_radius", restore);
      undoRedo.CommitAction();
    }

    if (shape is BoxShape3D box)
    {
      helper.BoxCommitHandle("Change Box Shape Size", cancel, node, shape);
    }

    if (shape is CapsuleShape3D capsule)
    {
      Vector2 values = (Vector2)restore;

      if (cancel)
      {
        capsule.Radius = values[0];
        capsule.Height = values[1];
        return;
      }

      if (handleId == 0)
      {
        undoRedo.CreateAction("Change Capsule Shape Radius");
        undoRedo.AddDoMethod(capsule, "set_radius", capsule.Radius);
      }
      else
      {
        undoRedo.CreateAction("Change Capsule Shape Height");
        undoRedo.AddDoMethod(capsule, "set_height", capsule.Height);
      }
      undoRedo.AddUndoMethod(capsule, "set_radius", values[0]);
      undoRedo.AddUndoMethod(capsule, "set_height", values[1]);
      undoRedo.CommitAction();
    }

    if (shape is CylinderShape3D cylinder)
    {
      if (cancel)
      {
        if (handleId == 0)
        {
          cylinder.Radius = (float)restore;
        }
        else
        {
          cylinder.Height = (float)restore;
        }
        return;
      }

      if (handleId == 0)
      {
        undoRedo.CreateAction("Change Cylinder Shape Radius");
        undoRedo.AddDoMethod(cylinder, "set_radius", cylinder.Radius);
        undoRedo.AddUndoMethod(cylinder, "set_radius", restore);
      }
      else
      {
        undoRedo.CreateAction("Change Cylinder Shape Height");
        undoRedo.AddDoMethod(cylinder, "set_height", cylinder.Height);
        undoRedo.AddUndoMethod(cylinder, "set_height", restore);
      }
      undoRedo.CommitAction();
    }

    if (shape is SeparationRayShape3D separationRay)
    {
      if (cancel)
      {
        separationRay.Length = (float)restore;
        return;
      }

      undoRedo.CreateAction("Change Separation Ray Shape Radius");
      undoRedo.AddDoMethod(separationRay, "set_length", separationRay.Length);
      undoRedo.AddUndoMethod(separationRay, "set_length", restore);
      undoRedo.CommitAction();
    }
  }

  public override void _Redraw(EditorNode3DGizmo gizmo)
  {
    PNGCollision node = (PNGCollision)gizmo.GetNode3D();
    gizmo.Clear();

    Shape3D shape = node.Shape;
    if (shape == null) return;

    var material = GetMaterial(node.Visible ? "shape_material" : "shape_material_disabled", gizmo);
    var handles_material = GetMaterial("handle_material");

    if (shape is SphereShape3D sphere)
    {
      float radius = sphere.Radius;

      var points = new List<Vector3>();
      for (int i = 0; i <= 360; i++)
      {
        float radianA = Mathf.DegToRad(i);
        float radianB = Mathf.DegToRad(i + 1);
        var a = new Vector2(Mathf.Sin(radianA), Mathf.Cos(radianA)) * radius;
        var b = new Vector2(Mathf.Sin(radianB), Mathf.Cos(radianB)) * radius;

        points.Add(new Vector3(a.X, 0, a.Y));
        points.Add(new Vector3(b.X, 0, b.Y));
        points.Add(new Vector3(0, a.X, a.Y));
        points.Add(new Vector3(0, b.X, b.Y));
        points.Add(new Vector3(a.X, a.Y, 0));
        points.Add(new Vector3(b.X, b.Y, 0));
      }

      var collisionSegments = new List<Vector3>();
      for (int i = 0; i < 64; i++)
      {
        float radianA = i * (Mathf.Tau / 64.0f);
        float radianB = (i + 1) * (Mathf.Tau / 64.0f);
        var a = new Vector2(Mathf.Sin(radianA), Mathf.Cos(radianA)) * radius;
        var b = new Vector2(Mathf.Sin(radianB), Mathf.Cos(radianB)) * radius;

        collisionSegments.Add(new Vector3(a.X, 0, a.Y));
        collisionSegments.Add(new Vector3(b.X, 0, b.Y));
        collisionSegments.Add(new Vector3(0, a.X, a.Y));
        collisionSegments.Add(new Vector3(0, b.X, b.Y));
        collisionSegments.Add(new Vector3(a.X, a.Y, 0));
        collisionSegments.Add(new Vector3(b.X, b.Y, 0));
      }

      var handles = new List<Vector3>
      {
          new(radius, 0, 0)
      };

      gizmo.AddLines(points.ToArray(), material);
      gizmo.AddCollisionSegments(collisionSegments.ToArray());
      gizmo.AddHandles(handles.ToArray(), handles_material, null);
    }

    if (shape is BoxShape3D box)
    {
      var lines = new List<Vector3>();
      var aabb = new Aabb
      {
        Position = -box.Size / 2,
        Size = box.Size
      };

      var point0 = aabb.GetEndpoint(0);
      var point1 = aabb.GetEndpoint(1);
      var point2 = aabb.GetEndpoint(2);
      var point3 = aabb.GetEndpoint(3);
      var point4 = aabb.GetEndpoint(4);
      var point5 = aabb.GetEndpoint(5);
      var point6 = aabb.GetEndpoint(6);
      var point7 = aabb.GetEndpoint(7);

      lines.Add(point0);
      lines.Add(point1);

      lines.Add(point1);
      lines.Add(point2);

      lines.Add(point2);
      lines.Add(point3);

      lines.Add(point0);
      lines.Add(point3);

      lines.Add(point0);
      lines.Add(point4);

      lines.Add(point1);
      lines.Add(point5);

      lines.Add(point2);
      lines.Add(point6);

      lines.Add(point3);
      lines.Add(point7);

      lines.Add(point4);
      lines.Add(point5);

      lines.Add(point5);
      lines.Add(point6);

      lines.Add(point6);
      lines.Add(point7);

      lines.Add(point4);
      lines.Add(point7);

      var handles = GizmoHelper.BoxGetHandles(box.Size);
      gizmo.AddLines(lines.ToArray(), material);
      gizmo.AddCollisionSegments(lines.ToArray());
      gizmo.AddHandles(handles.ToArray(), handles_material, null);
    }

    if (shape is CapsuleShape3D capsule)
    {
      float radius = capsule.Radius;
      float height = capsule.Height;

      var points = new List<Vector3>();
      var distance = new Vector3(0, height * 0.5f - radius, 0);
      for (int i = 0; i <= 360; i++)
      {
        float radianA = Mathf.DegToRad(i);
        float radianB = Mathf.DegToRad(i + 1);
        var a = new Vector2(Mathf.Sin(radianA), Mathf.Cos(radianA)) * radius;
        var b = new Vector2(Mathf.Sin(radianB), Mathf.Cos(radianB)) * radius;

        points.Add(new Vector3(a.X, 0, a.Y) + distance);
        points.Add(new Vector3(b.X, 0, b.Y) + distance);
        points.Add(new Vector3(a.X, 0, a.Y) - distance);
        points.Add(new Vector3(b.X, 0, b.Y) - distance);

        if (i % 90 == 0)
        {
          points.Add(new Vector3(a.X, 0, a.Y) + distance);
          points.Add(new Vector3(a.X, 0, a.Y) - distance);
        }

        var dud = i < 180 ? distance : -distance;

        points.Add(new Vector3(0, a.X, a.Y) + dud);
        points.Add(new Vector3(0, b.X, b.Y) + dud);
        points.Add(new Vector3(a.X, a.Y, 0) + dud);
        points.Add(new Vector3(b.X, b.Y, 0) + dud);
      }

      var collisionSegments = new List<Vector3>();
      for (int i = 0; i < 64; i++)
      {
        float radianA = i * (Mathf.Tau / 64.0f);
        float radianB = (i + 1) * (Mathf.Tau / 64.0f);
        var a = new Vector2(Mathf.Sin(radianA), Mathf.Cos(radianA)) * radius;
        var b = new Vector2(Mathf.Sin(radianB), Mathf.Cos(radianB)) * radius;

        collisionSegments.Add(new Vector3(a.X, 0, a.Y) + distance);
        collisionSegments.Add(new Vector3(b.X, 0, b.Y) + distance);
        collisionSegments.Add(new Vector3(a.X, 0, a.Y) - distance);
        collisionSegments.Add(new Vector3(b.X, 0, b.Y) - distance);

        if (i % 16 == 0)
        {
          collisionSegments.Add(new Vector3(a.X, 0, a.Y) + distance);
          collisionSegments.Add(new Vector3(a.X, 0, a.Y) - distance);
        }

        var dud = i < 32 ? distance : -distance;

        collisionSegments.Add(new Vector3(0, a.X, a.Y) + dud);
        collisionSegments.Add(new Vector3(0, b.X, b.Y) + dud);
        collisionSegments.Add(new Vector3(a.X, a.Y, 0) + dud);
        collisionSegments.Add(new Vector3(b.X, b.Y, 0) + dud);
      }

      var handles = new List<Vector3>
      {
          new(radius, 0, 0),
          new(0, height * 0.5f, 0)
      };

      gizmo.AddLines(points.ToArray(), material);
      gizmo.AddCollisionSegments(collisionSegments.ToArray());
      gizmo.AddHandles(handles.ToArray(), handles_material, null);
    }

    if (shape is CylinderShape3D cylinder)
    {
      float radius = cylinder.Radius;
      float height = cylinder.Height;

      var points = new List<Vector3>();
      var distance = new Vector3(0, height * 0.5f, 0);
      for (int i = 0; i <= 360; i++)
      {
        float radianA = Mathf.DegToRad(i);
        float radianB = Mathf.DegToRad(i + 1);
        var a = new Vector2(Mathf.Sin(radianA), Mathf.Cos(radianA)) * radius;
        var b = new Vector2(Mathf.Sin(radianB), Mathf.Cos(radianB)) * radius;

        points.Add(new Vector3(a.X, 0, a.Y) + distance);
        points.Add(new Vector3(b.X, 0, b.Y) + distance);
        points.Add(new Vector3(a.X, 0, a.Y) - distance);
        points.Add(new Vector3(b.X, 0, b.Y) - distance);

        if (i % 90 == 0)
        {
          points.Add(new Vector3(a.X, 0, a.Y) + distance);
          points.Add(new Vector3(a.X, 0, a.Y) - distance);
        }
      }

      var collisionSegments = new List<Vector3>();
      for (int i = 0; i < 64; i++)
      {
        float radianA = i * (Mathf.Tau / 64.0f);
        float radianB = (i + 1) * (Mathf.Tau / 64.0f);
        var a = new Vector2(Mathf.Sin(radianA), Mathf.Cos(radianA)) * radius;
        var b = new Vector2(Mathf.Sin(radianB), Mathf.Cos(radianB)) * radius;

        collisionSegments.Add(new Vector3(a.X, 0, a.Y) + distance);
        collisionSegments.Add(new Vector3(b.X, 0, b.Y) + distance);
        collisionSegments.Add(new Vector3(a.X, 0, a.Y) - distance);
        collisionSegments.Add(new Vector3(b.X, 0, b.Y) - distance);

        if (i % 16 == 0)
        {
          collisionSegments.Add(new Vector3(a.X, 0, a.Y) + distance);
          collisionSegments.Add(new Vector3(a.X, 0, a.Y) - distance);
        }
      }

      var handles = new List<Vector3>
      {
          new(radius, 0, 0),
          new(0, height * 0.5f, 0)
      };

      gizmo.AddLines(points.ToArray(), material);
      gizmo.AddCollisionSegments(collisionSegments.ToArray());
      gizmo.AddHandles(handles.ToArray(), handles_material, null);
    }

    if (shape is SeparationRayShape3D separationRay)
    {
      var points = new List<Vector3>
      {
          new(),
          new(0, 0, separationRay.Length)
      };
      var handles = new List<Vector3>
      {
          new(0, 0, separationRay.Length)
      };
      gizmo.AddLines(points.ToArray(), material);
      gizmo.AddCollisionSegments(points.ToArray());
      gizmo.AddHandles(handles.ToArray(), handles_material, null);
    }
  }
}
