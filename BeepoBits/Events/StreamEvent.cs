using Godot;
using Godot.Collections;

public partial class StreamEvent : BeepoEvent
{
  public StringName type;
  public Dictionary data = new();
}