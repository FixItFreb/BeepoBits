#if TOOLS
using Godot;
using System;

[Tool]
public partial class BeepoTwitch : EditorPlugin
{
	public static readonly string SingletonName = "BeepoBits_Twitch";
	public override void _EnterTree()
	{
		AddAutoloadSingleton(SingletonName, "./TwitchService.cs");
	}

	public override void _ExitTree()
	{
		RemoveAutoloadSingleton(SingletonName);
	}
}
#endif
