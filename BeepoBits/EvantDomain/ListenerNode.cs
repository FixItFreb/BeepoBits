using System;
using Godot;
public partial class ListenerNode : Node
{
  [Export] public StringName EventDomainID { get; private set; }
  public virtual void Notify(BeepoEvent beepoEvent) { }
}