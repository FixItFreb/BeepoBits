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
        
        if(collider is T tCollider)
        {
            colliderObject = tCollider;
            collider.Free();
            return true;
        }
        else
        {
            colliderObject = default(T);
            collider.Free();
            return false;
        }
    }
}

public static class Vector2Ext
{
    /// <summary>
    /// Returns a new Vector2 with the minimum X and Y values from v1 and v2
    /// </summary>
    /// <param name="v1"></param>
    /// <param name="v2"></param>
    /// <returns></returns>
    public static Vector2 Min(this Vector2 v1, Vector2 v2)
    {
        return new Vector2(Mathf.Min(v1.X, v2.X), Mathf.Min(v1.Y, v2.Y));
    }

    /// <summary>
    /// Returns a new Vector2 with the maximum X and Y values from v1 and v2
    /// </summary>
    /// <param name="v1"></param>
    /// <param name="v2"></param>
    /// <returns></returns>
    public static Vector2 Max(this Vector2 v1, Vector2 v2)
    {
        return new Vector2(Mathf.Max(v1.X, v2.X), Mathf.Max(v1.Y, v2.Y));
    }

    public static Vector2 WithX(this Vector2 v1, float newX)
    {
        return new Vector2(newX, v1.Y);
    }

    public static Vector2 WithY(this Vector2 v1, float newY)
    {
        return new Vector2(v1.X, newY);
    }

    public static Vector2I ToVector2I(this Vector2 v1)
    {
        return new Vector2I((int)v1.X, (int)v1.Y);
    }
}

public static class Vector3Ext
{
    /// <summary>
    /// Return this Vector3 with a x value of newX.
    /// </summary>
    /// <param name="v"></param>
    /// <param name="newX"></param>
    /// <returns></returns>
    public static Vector3 WithX(this Vector3 v, float newX)
    {
        return new Vector3(newX, v.Y, v.Z);
    }

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

    /// <summary>
    /// Return this Vector3 with a z value of newZ.
    /// </summary>
    /// <param name="v"></param>
    /// <param name="newZ"></param>
    /// <returns></returns>
    public static Vector3 WithZ(this Vector3 v, float newZ)
    {
        return new Vector3(v.X, v.Y, newZ);
    }

    public static Vector3 RandomPoint(this Vector3 v, double randX, double randY, double randZ)
    {
        return new Vector3(v.X + (float)GD.RandRange(randX * -1, randX), v.Y + (float)GD.RandRange(randY * -1, randY), v.Z + (float)GD.RandRange(randZ * -1, randZ));
    }

    public static Vector3 RandomPoint(this Vector3 v, Vector3 randV)
    {
        return new Vector3(v.X + (float)GD.RandRange(randV.X * -1, randV.X), v.Y + (float)GD.RandRange(randV.Y * -1, randV.Y), v.Z + (float)GD.RandRange(randV.Z * -1, randV.Z));
    }
}

public static class IEnumerableExt
{
    public delegate TResult TCondition<out TResult>(GodotObject input);

    /// <summary>
    /// Get element at index. Clamps index to between 0 and count - 1
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="source"></param>
    /// <param name="index"></param>
    /// <returns></returns>
    public static T GetIndexSafe<T>(this IEnumerable<T> source, int index)
    {
        if(source == null || source.Count() == 0)
        {
            return default(T);
        }
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

    /// <summary>
    /// Returns true if source IEnumerable contains all entries contained in toCheck
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="source"></param>
    /// <param name="toCheck"></param>
    /// <returns></returns>
    public static bool ContainsAll<T>(this IEnumerable<T> source, IEnumerable<T> toCheck)
    {
        foreach(T t in toCheck)
        {
            if(!source.Contains(t))
            {
                return false;
            }
        }
        return true;
    }

    /// <summary>
    /// Returns true is source IEnumerable contains any entries contained in toCheck
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="source"></param>
    /// <param name="toCheck"></param>
    /// <returns></returns>
    public static bool ContainsAny<T>(this IEnumerable<T> source, IEnumerable<T> toCheck)
    {
        foreach(T t in toCheck)
        {
            if(source.Contains(t))
            {
                return true;
            }
        }
        return false;
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
            if(a is T t)
            {
                areasOfType.Add(t);
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
        cast = node is T tNode ? tNode : null;
        return cast != null;
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

public static class GodotObjectExt
{
    /// <summary>
    /// Try to cast the godot object to the type T (where T inherits GodotObject).
    /// If successful returns true and castObject param will be the casted type.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="godotObject"></param>
    /// <param name="castObject"></param>
    /// <returns></returns>
    public static bool TryCast<T>(this GodotObject godotObject, out T castObject) where T : GodotObject
    {
        castObject = godotObject is T tObject ? tObject : null;
        return castObject != null;
    }
}

public static class AudioStreamPlayerExt
{
    public static void PlayAudio(this AudioStreamPlayer audioPlayer, AudioStream toPlay)
    {
        audioPlayer.Stream = toPlay;
        audioPlayer.Play();
    }
}