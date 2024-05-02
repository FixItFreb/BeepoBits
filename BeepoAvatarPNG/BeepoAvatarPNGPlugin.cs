#if TOOLS
using Godot;
using System;
using System.Net.NetworkInformation;

[Tool]
public partial class BeepoAvatarPNGPlugin : EditorPlugin
{
  // public static readonly string SingletonName = "BeepoAvatarPNGPlugin";
  private ShapeGizmo gizmo;

  public override void _EnterTree()
  {
    // AddAutoloadSingleton(SingletonName, "./BeepoCore.tscn");
    gizmo = new(GetUndoRedo());
    AddNode3DGizmoPlugin(gizmo);
  }

  public override void _ExitTree()
  {
    // RemoveAutoloadSingleton(SingletonName);
    RemoveNode3DGizmoPlugin(gizmo);
  }
}
#endif