using System.Diagnostics.CodeAnalysis;

namespace EECS.Core.Components;

/// <summary>
/// Attach this to struct that should be components.
/// </summary>
[AttributeUsage(AttributeTargets.Struct, Inherited = false)]
public sealed class ComponentAttribute : System.Attribute
{
    public ComponentAttribute() { }
}

/// <summary>
/// Instance belongs to a World.
/// </summary>
public class ComponentsManager
{
    private static Type[] _componentTypes;
    private static Dictionary<Type, int> _componentTypeIDs;
    
    public static IReadOnlyList<Type> ComponentTypes => _componentTypes;
    public static IReadOnlyDictionary<Type, int> ComponentTypeIDs => _componentTypeIDs;
    public readonly int MaxEntityID;
    public readonly World OfWorld;

    private IComponentPool[] _pools;

    /// <summary>
    /// Get all struct with ComponentAttribute and store them to _componentTypes and _componentTypeIDs.
    /// </summary>
    static ComponentsManager()
    {
        // Selected assembly version. I have no idea if it would be a good idea sometimes. Guess I'll reserve it for potential future use.
        // Assembly assembly = Assembly.GetExecutingAssembly();
        // var componentTypes =
        //     from t in assembly.GetTypes()
        //     let attributes = t.GetCustomAttributes(typeof(ComponentAttribute), true)
        //     where attributes != null && attributes.Length > 0
        //     select new { Type = t, Attributes = attributes.Cast<ComponentAttribute>() };
        // _componentTypes = componentTypes
        //     .Select(x => x.Type)
        //     .ToArray();

        
        var componentTypes =
            from a in AppDomain.CurrentDomain.GetAssemblies().AsParallel()
            from t in a.GetTypes()
            let attributes = t.GetCustomAttributes(typeof(ComponentAttribute), true)
            where attributes != null && attributes.Length > 0
            select new { Type = t, Attributes = attributes.Cast<ComponentAttribute>() };

        _componentTypes = componentTypes
            .Select(x => x.Type)
            .OrderBy(t => t.AssemblyQualifiedName)
            .ToArray();

        _componentTypeIDs = new Dictionary<Type, int>();
        for (int i = 0; i < _componentTypes.Length; i++)
            _componentTypeIDs[_componentTypes[i]] = i;
    }

    public ComponentsManager(World world, int maxEntityID)
    {
        OfWorld = world;
        MaxEntityID = maxEntityID;
        SetupComponentPools();
    }

    [MemberNotNull(nameof(_pools))]
    private void SetupComponentPools()
    {
        _pools = new IComponentPool[_componentTypes.Length];
        for(int i = 0; i < _componentTypes.Length; i++)
        {
            Type poolType = typeof(ComponentPool<>).MakeGenericType(_componentTypes[i]);
            // Console.WriteLine($"Attempting to construct Pool Type {poolType} of Component Type {_componentTypes[i]}");

            _pools[i] = (IComponentPool)Activator.CreateInstance(
                poolType,
                OfWorld,
                MaxEntityID,
                4096
            )!;
        }
    }

    /// <summary>
    /// Should be called along with ComponentsManager's Reset().
    /// </summary>
    internal void Reset()
    {
        foreach (var pool in _pools)
            pool.Reset();
    }

    internal void Free()
    {
        foreach (var pool in _pools)
            pool.Free();
    }

    public T Get<T>(Entity entity) where T : struct
    {
        return GetSet<T>().GetComponent(entity);
    }

    public ref T GetRef<T>(Entity entity) where T : struct
    {
        return ref GetSet<T>()[entity];
    }

    public void Set<T>(Entity entity, T component) where T : struct
    {
        GetSet<T>()[entity] = component;
    }

    public void Add<T>(Entity entity, T component) where T : struct
    {
        GetSet<T>().AddToEntity(entity, component);
    }
    public void Add<T>(Entity entity) where T : struct
    {
        Add<T>(entity, new T());
    }
 
    public void Remove<T>(Entity entity) where T : struct
    {
        GetSet<T>().RemoveFromEntity(entity);
    }

    public bool Has<T>(Entity entity) where T : struct
    {
        return GetSet<T>().EntityHasComponent(entity);
    }

    /// <summary>
    /// Returns a component pool.
    /// </summary>
    /// <typeparam name="T">Type of the component.</typeparam>
    /// <returns>Returns a ComponentPool object to manipulate the component with.</returns>
    public ComponentPool<T> GetSet<T>() where T : struct
    {
        int cid = _componentTypeIDs[typeof(T)];
        return (ComponentPool<T>)_pools[cid];
    }
    public IComponentPool GetSet(int componentID)
    {
        return _pools[componentID];
    }
    public IComponentPool GetSet(Type type)
    {
        return _pools[_componentTypeIDs[type]];
    }

    // TODO
    public string SerializeEntities()
    {
        return "";
    }

    public void DeserializeEntities(string s)
    {
        
    }
}
