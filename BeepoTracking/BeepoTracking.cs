#if TOOLS
using Godot;
using System;
using System.Net.NetworkInformation;

[Tool]
public partial class BeepoTracking : EditorPlugin
{
	public static readonly string SingletonName = "BeepoBits_Tracking";

	public override void _EnterTree()
	{
		AddAutoloadSingleton(SingletonName, "./BeepoTracking.tscn");
	}

	public override void _ExitTree()
	{
		RemoveAutoloadSingleton(SingletonName);
	}
}
#endif
