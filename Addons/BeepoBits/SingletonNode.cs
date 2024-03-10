using System;
using Godot;

public partial class SingletonNode<T> : Node where T : SingletonNode<T>
{
    protected static T _instance;
    public static T Instance { get { return _instance; } }

    public override void _EnterTree()
    {
        if(_instance != null)
        {
            this.QueueFree();
        }
        else
        {
            _instance = this as T;
        }
    }

    public override void _ExitTree()
    {
        if(_instance == this)
        {
            _instance = null;
        }
    }
}