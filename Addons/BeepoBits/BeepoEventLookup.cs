using Godot;

[GlobalClass]
public partial class BeepoEventLookup : Resource
{
    public virtual StringName eventDomainID { get; }
    [Export] public StringName eventTriggerID;
}