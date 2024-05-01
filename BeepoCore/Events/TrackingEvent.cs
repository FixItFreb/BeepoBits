using System.Collections.Generic;
using Godot;
using Godot.Collections;

public partial class BoneTrackingEvent : BeepoEvent
{
  public string name;
  public Vector3 pos;
  public Quaternion rot;
}


public struct BlendShapeTrackingData
{
  public uint hash;
  public float value;
}

public partial class BlendShapeTrackingEvent : BeepoEvent
{
  public List<BlendShapeTrackingData> data;
}