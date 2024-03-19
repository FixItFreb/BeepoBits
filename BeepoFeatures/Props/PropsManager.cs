using Godot;
using Godot.Collections;
using System;

public partial class PropsManager : EventDomainNode<PropsManager>, IEventDomain
{
    [Export] public Dictionary<StringName, NodePath> propAnchors = new Dictionary<StringName, NodePath>();
    [Export] private Dictionary<StringName, Prop> currentProps = new Dictionary<StringName, Prop>();

    public void RegisterProp(Prop toAdd)
    {
        if (!currentProps.ContainsKey(toAdd.propName))
        {
            currentProps.Add(toAdd.propName, toAdd);
        }
        else
        {
            GD.PrintErr("Trying to add prop [" + toAdd.propName + "] but prop with same name is already registered.");
        }
    }

    public void UnregisterProp(Prop toRemove)
    {
        currentProps.Remove(toRemove.propName);
    }

    public void SetPropActive(string propName, bool isActive)
    {
        if (currentProps.TryGetValue(propName, out Prop prop))
        {
            prop.SetActive(isActive);
        }
    }

    public void AttachProp(StringName propName, StringName anchorName, float duration = 0)
    {
        if (currentProps.TryGetValue(propName, out Prop prop))
        {
            if(prop == null)
            {
                GD.Print("uwotm8");
            }
            prop.AttachToNode(anchorName, duration);
        }
    }
}
