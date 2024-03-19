using Godot;
using System.Collections.Generic;

public partial class GizmoHelper : GodotObject
{
  private Variant initialValue;
  private Transform3D initialTransform;
  private EditorUndoRedoManager undoRedo;

  public GizmoHelper(EditorUndoRedoManager manager)
  {
    undoRedo = manager;
  }

  public void InitializeHandleAction(Variant value, Transform3D transform)
  {
    initialValue = value;
    initialTransform = transform;
  }

  public Vector3[] GetSegment(Camera3D camera, Vector2 point)
  {
    var inverse = initialTransform.AffineInverse();

    var rayFrom = camera.ProjectRayOrigin(point);
    var rayDir = camera.ProjectRayNormal(point);

    Vector3[] segment = new Vector3[2];
    segment[0] = Xform(inverse.Basis, inverse.Origin, rayFrom);
    segment[1] = Xform(inverse.Basis, inverse.Origin, rayFrom + rayDir * 4096);

    return segment;
  }

  static public List<Vector3> BoxGetHandles(Vector3 boxSize)
  {
    var handles = new List<Vector3>();
    for (int i = 0; i < 3; i++)
    {
      var axis = new Vector3();
      axis[i] = boxSize[i] / 2;
      handles.Add(axis);
      handles.Add(-axis);
    }

    return handles;
  }

  static public string BoxGetHandleName(int handleId)
  {
    switch (handleId)
    {
      case 0:
      case 1:
        return "Size X";
      case 2:
      case 3:
        return "Size Y";
      case 4:
      case 5:
        return "Size Z";
      default:
        return "";
    }
  }

  public void BoxSetHandle(Vector3[] segment, int handleId, out Vector3 boxSize, out Vector3 boxPosition)
  {
    int axis = handleId / 2;
    int sign = handleId % 2 * -2 + 1;

    var initialSize = initialValue.AsVector3();
    float negEnd = initialSize[axis] * -0.5f;
    float posEnd = initialSize[axis] * 0.5f;

    Vector3[] axisSegment = { new Vector3(), new Vector3() };
    axisSegment[0][axis] = 4096.0f;
    axisSegment[1][axis] = -4096.0f;
    var points = Geometry3D.GetClosestPointsBetweenSegments(axisSegment[0], axisSegment[1], segment[0], segment[1]);
    var pointA = points[0];
    var pointB = points[1];

    // Calculate new size.
    boxSize = initialSize;
    if (Input.IsKeyPressed(Key.Alt))
    {
      boxSize[axis] = pointA[axis] * sign * 2;
    }
    else
    {
      boxSize[axis] = sign > 0 ? pointA[axis] - negEnd : posEnd - pointA[axis];
    }

    // Adjust position.
    if (Input.IsKeyPressed(Key.Alt))
    {
      boxPosition = initialTransform.Origin;
    }
    else
    {
      if (sign > 0)
      {
        posEnd = negEnd + boxSize[axis];
      }
      else
      {
        negEnd = posEnd - boxSize[axis];
      }

      var offset = new Vector3();
      offset[axis] = (posEnd + negEnd) * 0.5f;
      boxPosition = Xform(initialTransform.Basis, initialTransform.Origin, offset);
    }
  }

  public void BoxCommitHandle(string actionName, bool cancel, GodotObject positionObject, GodotObject sizeObject, string positionProperty = "global_position", string sizeProperty = "size")
  {
    sizeObject ??= positionObject;

    if (cancel)
    {
      sizeObject.Set(sizeProperty, initialValue);
      positionObject.Set(positionProperty, initialTransform.Origin);
      return;
    }

    undoRedo.CreateAction(actionName);
    undoRedo.AddDoProperty(sizeObject, sizeProperty, sizeObject.Get(sizeProperty));
    undoRedo.AddDoProperty(positionObject, positionProperty, positionObject.Get(positionProperty));
    undoRedo.AddUndoProperty(sizeObject, sizeProperty, initialValue);
    undoRedo.AddUndoProperty(positionObject, positionProperty, initialTransform.Origin);
    undoRedo.CommitAction();
  }

  static private Vector3 Xform(Basis basis, Vector3 origin, Vector3 point)
  {
    return new(
      basis[0].Dot(point) + origin.X,
      basis[1].Dot(point) + origin.Y,
      basis[2].Dot(point) + origin.Z
    );
  }
}
