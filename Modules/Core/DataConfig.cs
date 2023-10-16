using Godot;
using Godot.Collections;

[Tool]
public partial class DataConfig : Resource
{
    [Export] protected Dictionary<string, PackedScene> data = new Dictionary<string, PackedScene>();
    public Dictionary<string, PackedScene> Data { get { return data; } }


    public bool TryGetEntry(string entryName, out PackedScene entry)
    {
        entry = null;
        if (data.TryGetValue(entryName, out PackedScene p))
        {
            entry = p;
        }
        return entry != null;
    }

    public bool TryInstantiateEntry<T>(string entryName, out T entry) where T : GodotObject
    {
        entry = null;
        if (data.TryGetValue(entryName, out PackedScene p))
        {
            //GD.Print("Found: " + entryName);
            entry = p.Instantiate<T>();
        }
        else
        {
            GD.PrintErr("Did not find: " + entryName);
        }
        return entry != null;
    }

    public T GetRandom<T>() where T : GodotObject
    {
        PackedScene p = data.GetRandom().Value;
        if (p is T)
        {
            return p as T;
        }
        else
        {
            return null;
        }
    }

    public bool TryInstantiateRandomEntry<T>(out T entry) where T : GodotObject
    {
        entry = null;
        PackedScene random = data.GetRandom().Value;
        if(random is T)
        {
            entry = random.Instantiate<T>();
        }
        return entry != null;
    }
}
