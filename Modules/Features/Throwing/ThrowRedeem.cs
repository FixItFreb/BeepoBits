using Godot;
using Godot.Collections;
using System;

public enum ThrowType
{
    Throw,
    Drop
}

[GlobalClass]
public partial class ThrowRedeem : ChannelRedeemNode
{
    [Export] public int throwCount = 1;
    [Export] public Array<StringName> toThrow = new Array<StringName>();
    [Export] public ThrowType throwType = ThrowType.Throw;

    public override void ExecuteChannelRedeem(TwitchRedeemPayload payload)
    {
        if (CheckRedeem(payload))
        {
            if (throwType == ThrowType.Throw)
            {
                ThrowingManager.Instance.Throw(throwCount, toThrow);
            }
            else
            {
                ThrowingManager.Instance.DropObject(toThrow);
            }
        }
    }
}
