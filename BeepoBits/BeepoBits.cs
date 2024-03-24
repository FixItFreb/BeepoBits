#if TOOLS
using Godot;
using System;

[Tool]
public partial class BeepoBits : EditorPlugin
{
	Node3D coreScene;

	public override void _EnterTree()
	{
		AddAutoloadSingleton("BeepoBits_Core", "./BeepoCore.tscn");
	}

	public override void _ExitTree()
	{
		RemoveAutoloadSingleton("BeepoBits_Core");
	}
}
#endif
