using Godot;
using System;

public partial class BeepoAvatarPNG : RigidBody3D
{
    [Export] private BeepoAvatar avatar;
    public BeepoAvatar Avatar { get { return avatar; } }

    [Export] private Sprite3D avatarSprite;
    public Sprite3D AvatarSprite { get { return avatarSprite; } }

    [Export] private CollisionShape3D collider;
}
