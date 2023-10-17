using Godot;
using System;

public partial class Prop : RemoteTransform3D
{
    [Export] public string propName;
    [Export] public Node3D attachedTo;

    public override void _Ready()
    {
        PropsManager.Instance.RegisterProp(this);
    }

    public override void _ExitTree()
    {
        PropsManager.Instance.UnregisterProp(this);
    }

    public void AttachToNode(NodePath nodePath)
    {
        Node3D attachTo = GetNode<Node3D>(nodePath);
        if (attachTo != null)
        {
            attachedTo = GetNode<Node3D>(nodePath);
            RemotePath = nodePath;
        }
    }

    public void AttachToNode(Node3D attachTo)
    {
        attachedTo = attachTo;
        RemotePath = attachTo.GetPath();
    }

    public void SetActive(bool isActive)
    {
        Visible = isActive;
        SetProcess(isActive);
    }
}