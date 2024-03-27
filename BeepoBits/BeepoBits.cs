#if TOOLS
using Godot;
using System;
using System.Net.NetworkInformation;

[Tool]
public partial class BeepoBits : EditorPlugin
{
	public static readonly string SingletonName = "BeepoBits_Core";

	public override void _EnterTree()
	{
		AddAutoloadSingleton(SingletonName, "./BeepoCore.tscn");
	}

	public override void _ExitTree()
	{
		RemoveAutoloadSingleton(SingletonName);
	}
}
#endif
