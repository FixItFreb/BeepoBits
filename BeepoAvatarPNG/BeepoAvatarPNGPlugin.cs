#if TOOLS
using Godot;
using System;
using System.Net.NetworkInformation;

[Tool]
public partial class BeepoAvatarPNGPlugin : EditorPlugin
{
  // public static readonly string SingletonName = "BeepoAvatarPNGPlugin";

  public override void _EnterTree()
  {
    // AddAutoloadSingleton(SingletonName, "./BeepoCore.tscn");
  }

  public override void _ExitTree()
  {
    // RemoveAutoloadSingleton(SingletonName);
  }
}
#endif