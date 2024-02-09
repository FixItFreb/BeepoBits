using Godot;
using System;

public partial class ThrowingLauncher : Node3D
{
    //private Tween throwTween;
    //[Export] private float tweenDuration = 1.0f;

    [Export] public Vector3 targetDeviation = new Vector3(0.1f, 0.1f, 0);
    [Export] public Vector3 launchDeviation = new Vector3(0.1f, 0.5f, 0);

    public void ThrowObject()
    {
        if(BeepoCore.currentAvatars.Count == 0)
        {
            return;
        }
        GD.Print(Name);
        BeepoAvatar target = BeepoCore.currentAvatars.GetRandom();
        Throwable toThrow = ThrowingManager.Instance.throwables.GetRandom<PackedScene>().Instantiate<Throwable>();
        ThrowingManager.Instance.AddChild(toThrow);
        toThrow.GlobalPosition = GlobalPosition.RandomPoint(launchDeviation);
        toThrow.Rotation = new Vector3(GD.RandRange(0, 360), GD.RandRange(0, 360), GD.RandRange(0, 360));
        Tween throwTween = CreateTween();
        toThrow.SetTweenPath(throwTween);
        Vector3 startPostion = toThrow.GlobalPosition;
        Vector3 targetPostion = target.headPostion.GlobalPosition.RandomPoint(targetDeviation);
        double tweenDuration = GD.RandRange(0.4f, 0.8f);
        throwTween.TweenMethod(Callable.From((float t) =>
        {
            if (!toThrow.FirstCollision)
            {
                toThrow.GlobalPosition = startPostion.Lerp(targetPostion, t);
                if (t >= 0.9f)
                {
                    toThrow.DetachFromTweenPath();
                }
            }
        }), 0.0f, 1.0f, tweenDuration);
    }
}
