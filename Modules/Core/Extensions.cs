using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Godot.Collections;

public static class RayCast3DExt
{
    /// <summary>
    /// Get the first collider this ray intersects with and try casting it to type T (where T inherits GodotObject).
    /// If successful returns true and colliderObject param is set to the casted type.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="rayCast3D"></param>
    /// <param name="colliderObject"></param>
    /// <returns></returns>
    public static bool TryGetCollider<T>(this RayCast3D rayCast3D, out T colliderObject) where T : GodotObject
    {
        GodotObject collider = rayCast3D.GetCollider();
        if(collider == null)
        {
            colliderObject = default(T);
            return false;
        }
        
        if(collider is T)
        {
            colliderObject = collider as T;
            return true;
        }
        else
        {
            colliderObject = default(T);
            return false;
        }
    }
}

public static class Vector3Ext
{
    /// <summary>
    /// Return this Vector3 with a y value of newY.
    /// </summary>
    /// <param name="v"></param>
    /// <param name="newY"></param>
    /// <returns></returns>
    public static Vector3 WithY(this Vector3 v, float newY)
    {
        return new Vector3(v.X, newY, v.Z);
    }

    public static Vector3 RandomPoint(this Vector3 v, double randX, double randY, double randZ)
    {
        return new Vector3(v.X + (float)GD.RandRange(randX * -1, randX), v.Y + (float)GD.RandRange(randY * -1, randY), v.Z + (float)GD.RandRange(randZ * -1, randZ));
    }
}

public static class IEnumerableExt
{
    /// <summary>
    /// Get element at index. Clamps index to between 0 and count - 1
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="source"></param>
    /// <param name="index"></param>
    /// <returns></returns>
    public static T GetIndexSafe<T>(this IEnumerable<T> source, int index)
    {
        index = Mathf.Clamp(index, 0, source.Count() - 1);
        return source.ElementAt(index);
    }

    /// <summary>
    /// Returns a random element between index 0 and Count - 1
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="source"></param>
    /// <returns></returns>
    public static T GetRandom<T>(this IEnumerable<T> source)
    {
        return source.ElementAt(GD.RandRange(0, source.Count() - 1));
    }

    /// <summary>
    /// Returns true if the given IEnumerable is null or has Count of 0
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="source"></param>
    /// <returns></returns>
    public static bool IsNullOrEmpty<T>(this IEnumerable<T> source)
    {
        return source == null || source.Count() == 0;
    }
}

public static class IListExt
{
    /// <summary>
    /// Shuffles this list.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="source"></param>
    public static void Shuffle<T>(this IList<T> source)
    {
        for (int i = source.Count - 1; i > 0; i--)
        {
            int r = Random.Shared.Next(0, i + 1);
            T temp = source[i];
            source[i] = source[r];
            source[r] = temp;
        }
    }
}

public static class Area3DExt
{
    /// <summary>
    /// Returns a Godot Array of type T (where T inherits GodotObject) containing all areas of type T this node is currently overlapping.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="area3D"></param>
    /// <returns></returns>
    public static Array<T> GetOverlappingAreasOfType<[MustBeVariant] T>(this Area3D area3D) where T : GodotObject
    {
        Array<T> areasOfType = new Array<T>();
        Array<Area3D> areas = area3D.GetOverlappingAreas();
        foreach(Area3D a in areas)
        {
            if(a is T)
            {
                areasOfType.Add(a as T);
            }
        }
        return areasOfType;
    }
}

public static class NodeExt
{
    public delegate TResult TimerConditional<out TResult>();

    /// <summary>
    /// Try to cast the node to the type T (where T is a class).
    /// If successful returns true and cast param will be the casted type.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="node"></param>
    /// <param name="cast"></param>
    /// <returns></returns>
    public static bool TryCastBase<T>(this Node node, out T cast) where T : class
    {
        cast = null;
        if (node is T)
        {
            cast = node as T;
        }
        return cast != null;
    }

    /// <summary>
    /// Try to cast the node to the type T (where T inherits GodotObject).
    /// If successful returns true and castNode param will be the casted type.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="node"></param>
    /// <param name="castNode"></param>
    /// <returns></returns>
    public static bool TryCast<[MustBeVariant] T>(this Node node, out T castNode) where T : GodotObject
    {
        castNode = null;
        if(node is T)
        {
            castNode = node as T;
        }
        return castNode != null;
    }

    /// <summary>
    /// Adds a timer to this node that will execute the given onTimeout method when the timer ticks.
    /// </summary>
    /// <param name="node"></param>
    /// <param name="waitTime"></param>
    /// <param name="oneShot"></param>
    /// <param name="onPhysics"></param>
    /// <param name="onTimeout"></param>
    /// <returns></returns>
    public static Timer SetupTimer(this Node node, float waitTime, bool oneShot, bool onPhysics = false, Action onTimeout = null)
    {
        Timer timer = new Timer();
        timer.Autostart = true;
        timer.ProcessCallback = onPhysics ? Timer.TimerProcessCallback.Physics : Timer.TimerProcessCallback.Idle;
        timer.WaitTime = waitTime;
        timer.OneShot = oneShot;
        if (onTimeout != null)
        {
            timer.Timeout += onTimeout;
        }
        node.AddChild(timer);
        return timer;
    }

    /// <summary>
    /// Sets a timer that will repeatedly check the given conditional lambda function until it passes, at which point onTrue will be called.
    /// </summary>
    /// <param name="node"></param>
    /// <param name="conditional"></param>
    /// <param name="onTrue"></param>
    /// <param name="callDeferred"></param>
    /// <returns></returns>
    public static Timer SetupConditionalTimer(this Node node, TimerConditional<bool> conditional, Callable onTrue, bool callDeferred = false)
    {
        Timer timer = new Timer();
        timer.Autostart = true;
        timer.ProcessCallback = Timer.TimerProcessCallback.Physics;
        timer.WaitTime = 0.1f;
        timer.OneShot = false;
        timer.Timeout += () =>
        {
            if (conditional())
            {
                //onTrue();
                if(callDeferred)
                {
                    onTrue.CallDeferred();
                }
                else
                {
                    onTrue.Call();
                }
                timer.Stop();
            }
        };
        node.AddChild(timer);
        return timer;
    }
}

public static class PackedSceneExt
{
    public static bool TryInstantiate<T>(this PackedScene packedScene, out T castPackedScene) where T : GodotObject
    {
        castPackedScene = null;
        if (packedScene is T)
        {
            castPackedScene = packedScene.Instantiate<T>();
        }
        return castPackedScene != null;
    }
}

public static class Path3DExt
{
    public static void SetPointGlobalPosition(this Path3D path3D, int pointIndex, Vector3 newGlobalPosition)
    {
        path3D.Curve.SetPointPosition(pointIndex, path3D.GlobalPosition + newGlobalPosition);
    }
}