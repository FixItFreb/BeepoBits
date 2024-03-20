#if TOOLS
using Godot;

[Tool]
public partial class ShapeGizmoPlugin : EditorPlugin
{
	private ShapeGizmo gizmo;

	public override void _EnterTree()
	{
		gizmo = new(GetUndoRedo());
		AddNode3DGizmoPlugin(gizmo);
	}

	public override void _ExitTree()
	{
		RemoveNode3DGizmoPlugin(gizmo);
	}
}
#endif
