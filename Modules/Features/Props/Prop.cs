using Godot;
using System;

public partial class Prop : Node3D
{
    [Export] public StringName propName;
    [Export] public Node3D attachedTo;

    private float lifeTime = 0;

    public override void _Ready()
    {
        PropsManager.Instance.RegisterProp(this);
        SetActive(false);
    }

    public override void _ExitTree()
    {
        PropsManager.Instance.UnregisterProp(this);
    }

    public override void _Process(double delta)
    {
        if(attachedTo != null)
        {
            GlobalTransform = attachedTo.GlobalTransform;
            
            if (lifeTime > 0)
            {
                lifeTime -= (float)delta;
                if (lifeTime <= 0)
                {
                    SetActive(false);
                }
            }
        }
    }

    public void AttachToNode(StringName anchorName, float duration = 0)
    {
        if(PropsManager.Instance.propAnchors.TryGetValue(anchorName, out NodePath anchor))
        {
            attachedTo = PropsManager.Instance.GetNode<Node3D>(anchor);
            if (duration > 0)
            {
                lifeTime = duration;
            }
            SetActive(true);
        }
    }

    public void SetActive(bool isActive)
    {
        if (!isActive)
        {
            attachedTo = null;
        }
        Visible = isActive;
        SetProcess(isActive);
    }
}