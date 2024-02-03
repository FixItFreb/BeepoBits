using Godot;
using Godot.Collections;

[GlobalClass]
public partial class TrailGradientSet : Resource
{
    [Export] public Array<Gradient> gradients = new Array<Gradient>();
}
