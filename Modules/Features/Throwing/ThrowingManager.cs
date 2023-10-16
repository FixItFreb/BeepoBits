using Godot;
using Godot.Collections;

public partial class ThrowingManager : Node3D
{
    private static ThrowingManager _instance;
    public static ThrowingManager Instance { get { return _instance; } }

    [Export] public PackedScene throwPathPackedScene;
    [Export] public ThrowingLauncher[] launchers;
    [Export] public Array<PackedScene> throwables;

    public override void _EnterTree()
    {
        _instance = this;
    }

    public override void _Process(double delta)
    {
        if(Input.IsActionJustPressed("debug_action"))
        {
            GD.Print("Throwing");
            Throw();
        }
    }

    public void Throw(int throwCount = 1)
    {
        if (throwCount == 1)
        {
            launchers.GetRandom().ThrowObject();
        }
        else
        {
            ThrowMulti(throwCount);
        }
    }

    private async void ThrowMulti(int throwCount)
    {
        for (int i = 0; i < throwCount; i++)
        {
            launchers.GetRandom().ThrowObject();
            await ToSignal(GetTree().CreateTimer(GD.RandRange(0.2, 0.3)), Timer.SignalName.Timeout);
        }
    }
}
