using Godot;
using System;

public partial class Toolbox : Node
{
    [Export] private TextEdit debugTextWindow;

    public override void _Ready()
    {
        BeepoCore.GetInstance().Connect(BeepoCore.SignalName.OnDebugLog, Callable.From((string debugString) => { DebugLog(debugString); }));
    }

    private void DebugLog(string debugString)
    {
        debugTextWindow.Text += string.Format("{0}\n", debugString);
        debugTextWindow.ScrollVertical = Int32.MaxValue;
    }
}
