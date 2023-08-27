using Godot;
using System;

public class RaidEventPayload
{
    public string raiderUsername;
    public string raiderDisplayname;
    public string raidersCount;

    public RaidEventPayload(string _raiderUsername, string _raiderDisplayname, string _raidersCount)
    {
        raiderUsername = _raiderUsername;
        raiderDisplayname = _raiderDisplayname;
        raidersCount = _raidersCount;
    }
}

public partial class RaidEventNode : Node
{
    protected BeepoCore beepoCore;

    public override void _Ready()
    {
        beepoCore = GetNode<BeepoCore>("%BeepoCore");
        beepoCore.RegisterRaidEvent(this);
    }

    public override void _ExitTree()
    {
        beepoCore.UnregisterRaidEvent(this);
    }

    public virtual void ExecuteRaidEvent(RaidEventPayload payload)
    {
    }
}
