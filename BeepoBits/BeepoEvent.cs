using Godot;

public partial class BeepoEvent : Resource
{
    public virtual StringName eventDomainID { get; }
    [Export] public StringName eventTriggerID;
}