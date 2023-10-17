using Godot;
using Godot.Collections;
using System;

public partial class PropsManager : Node
{
    private static PropsManager _instance;
    public static PropsManager Instance { get { return _instance; } }

    [Export] private Dictionary<string, Prop> currentProps = new Dictionary<string, Prop>();

    public override void _EnterTree()
    {
        _instance = this;
    }

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

    public void AttachProp(string propName, string nodePath)
    {
        if (currentProps.TryGetValue(propName, out Prop prop))
        {
            prop.AttachToNode(nodePath);
        }
    }
}
