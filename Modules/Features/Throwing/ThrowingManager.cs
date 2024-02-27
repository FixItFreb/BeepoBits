using Godot;
using Godot.Collections;

public partial class ThrowingManager : EventDomainNode<ThrowingManager>, IEventDomain
{
    [Export] public PackedScene throwPathPackedScene;
    [Export] public ThrowingLauncher[] launchers;
    [Export] public float dropHeight = 5.0f;

    [Export] public Dictionary<StringName, PackedScene> throwables = new Dictionary<StringName, PackedScene>();
    [Export] public Dictionary<StringName, PackedScene> dropables = new Dictionary<StringName, PackedScene>();

    public void Throw(int throwCount = 1, Array<StringName> toThrow = null)
    {
        StringName selectedToThrow = null;
        if(!toThrow.IsNullOrEmpty())
        {
            selectedToThrow = toThrow.GetRandom();
        }

        if (throwCount == 1)
        {
            launchers.GetRandom().ThrowObject(selectedToThrow);
        }
        else
        {
            ThrowMulti(throwCount, selectedToThrow);
        }
    }

    private async void ThrowMulti(int throwCount, StringName toThrow = null)
    {
        for (int i = 0; i < throwCount; i++)
        {
            launchers.GetRandom().ThrowObject(toThrow);
            await ToSignal(GetTree().CreateTimer(GD.RandRange(0.2, 0.3)), Timer.SignalName.Timeout);
        }
    }

    public void DropObject(Array<StringName> toDrop = null)
    {
        if (BeepoCore.currentAvatars.Count == 0)
        {
            return;
        }

        StringName selectedToDrop = null;
        if (!toDrop.IsNullOrEmpty())
        {
            selectedToDrop = toDrop.GetRandom();
        }

        BeepoAvatar target = BeepoCore.currentAvatars.GetRandom();
        Throwable toThrow = GetDropable(selectedToDrop).Instantiate<Throwable>();
        BeepoCore.WorldRoot.AddChild(toThrow);
        toThrow.GlobalPosition = target.headPostion.GlobalPosition.WithY(dropHeight);
        toThrow.Rotation = new Vector3(Mathf.DegToRad(GD.RandRange(-10, 10)), Mathf.DegToRad(GD.RandRange(0, 360)), Mathf.DegToRad(GD.RandRange(10, 10)));
    }

    public PackedScene GetThrowable(StringName throwID)
    {
        if (throwID != null && throwables.TryGetValue(throwID, out PackedScene t))
        {
            return t;
        }
        else
        {
            return throwables.Values.GetRandom(); ;
        }
    }

    public PackedScene GetDropable(StringName dropID)
    {
        if (dropID != null && dropables.TryGetValue(dropID, out PackedScene t))
        {
            return t;
        }
        else
        {
            return dropables.Values.GetRandom(); ;
        }
    }
}
