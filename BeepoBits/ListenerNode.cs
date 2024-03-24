using System;
using Godot;
public partial class ListenerNode : Node
{
  public virtual void Notify(BeepoEvent beepoEvent) { }
}