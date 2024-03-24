using Godot;
using System;

public enum BeepoAvatarType
{
    AvatarPNG,
    AvatarVRM,
    AvatarInochi,
}

[GlobalClass]
public partial class BeepoAvatar : Node3D
{
    [Export] public Node3D headPostion;

    [Export] private BeepoAvatarType avatarType = BeepoAvatarType.AvatarPNG;
    public BeepoAvatarType AvatarType { get { return avatarType; } }

    [Export] private Node avatarNode;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        // BeepoCore.RegisterNewAvatar(this);
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
    {
    }

    public override void _ExitTree()
    {
        BeepoCore.currentAvatars.Remove(this);
    }

    public T GetAvatarNode<T>()
    {
        if (avatarNode is T tNode)
        {
            return tNode;
        }
        else
        {
            return default(T);
        }
    }
}
