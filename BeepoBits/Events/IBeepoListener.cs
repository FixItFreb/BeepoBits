using System;
using Godot;
public interface IBeepoListener
{
  void Notify(BeepoEvent beepoEvent) { }
}