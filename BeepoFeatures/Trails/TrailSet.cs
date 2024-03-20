using Godot;
using Godot.Collections;

public partial class TrailSet : Node
{
    [Export] public StringName setName;
    [Export] public Array<TrailEffect> trails = new Array<TrailEffect>();
    [Export] public Array<TrailGradientSet> trailGradients = new Array<TrailGradientSet>();
    [Export] public float maxDuration = 120.0f;

    private float durationRemaning = 0;

    public void Trigger(float duration)
    {
        durationRemaning = Mathf.Min(durationRemaning + duration, maxDuration);
        SetState(true);
    }

    public void Toggle()
    {
        TrailGradientSet gradientSet = trailGradients.GetRandom();
        GD.Print("gradientSetCount: " + gradientSet.gradients.Count);
        for (int i = 0; i < trails.Count; i++)
        {
            trails[i].trailGradient = gradientSet.gradients[i];
            trails[i].Enabled = !trails[i].Enabled;
        }
    }

    public void SetState(bool state)
    {
        if(state)
        {
            TrailGradientSet gradientSet = trailGradients.GetRandom();
            for (int i = 0; i < trails.Count; i++)
            {
                trails[i].trailGradient = gradientSet.gradients[i];
                trails[i].Enabled = true;
            }
        }
        else
        {
            foreach (TrailEffect t in trails)
            {
                t.Enabled = false;
            }
        }
    }

    public override void _Process(double delta)
    {
        if (durationRemaning > 0)
        {
            durationRemaning -= (float)delta;
            if (durationRemaning <= 0)
            {
                SetState(false);
            }
        }
    }
}