using System.Runtime.CompilerServices;
using System.Text;

namespace EECS.Core.Components;

/// <summary>
/// Attach this to struct that should be components.
/// For closed generic component types, use
/// <see cref="ComponentsManager.RegisterComponent{T}(string?)"/>.
/// </summary>
[AttributeUsage(AttributeTargets.Struct, Inherited = false)]
public sealed class ComponentAttribute : Attribute
{
    public string? Name;
    public ComponentAttribute(string name)
    {
        Name = name;
    }
    public ComponentAttribute()
    {
        Name = null;
    }
}

/// <summary>
/// Instance belongs to a World.
/// </summary>
public partial class ComponentsManager
{
    public delegate void ComponentAddedDelegate(World world, Entity entity, Type componentType);
    public delegate void ComponentRemovedDelegate(World world, Entity entity, Type componentType);

    private static readonly List<Type> _componentTypes;
    private static readonly Dictionary<Type, int> _componentIDByType;
    private static readonly Dictionary<string, int> _componentIDByName;
    private static readonly Dictionary<string, Type> _componentTypeByName;
    private static readonly Dictionary<Type, string> _componentNameByType;
    private static readonly object _componentRegistrationLock = new();
    private static readonly List<WeakReference<ComponentsManager>> _managers = new();
    internal static readonly Dictionary<Type, ComponentAddedDelegate> ComponentAddedDelegates = new();
    internal static readonly Dictionary<Type, ComponentRemovedDelegate> ComponentRemovedDelegates = new();

    public readonly static IReadOnlyList<Type> ComponentTypes;
    public readonly static IReadOnlyDictionary<Type, int> ComponentIDByType;
    public readonly static IReadOnlyDictionary<string, int> ComponentIDByName;
    public readonly static IReadOnlyDictionary<string, Type> ComponentTypeByName;
    public readonly static IReadOnlyDictionary<Type, string> ComponentNameByType;
    public readonly int MaxEntityCount;
    public readonly World OfWorld;

    private readonly List<IComponentPool> _pools;

    /// <summary>
    /// Get all struct with ComponentAttribute and store them to _componentTypes and _componentIDByType.
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
            where !t.ContainsGenericParameters && attributes != null && attributes.Length > 0
            select new { Type = t, Attributes = attributes.Cast<ComponentAttribute>() };

        _componentTypes = componentTypes
            .Select(x => x.Type)
            .OrderBy(t => t.AssemblyQualifiedName)
            .ToList();

        var explicitComponentNames = new Dictionary<Type, string>();
        foreach (var componentType in _componentTypes)
        {
            var attribute = componentType
                .GetCustomAttributes(typeof(ComponentAttribute), true)
                .Cast<ComponentAttribute>()
                .FirstOrDefault();

            if (attribute != null && !string.IsNullOrWhiteSpace(attribute.Name))
                explicitComponentNames[componentType] = attribute.Name;
        }

        _componentIDByType = new Dictionary<Type, int>();
        for (int i = 0; i < _componentTypes.Count; i++)
            _componentIDByType[_componentTypes[i]] = i;

        _componentIDByName = new Dictionary<string, int>();
        _componentNameByType = new Dictionary<Type, string>();
        _componentTypeByName = new Dictionary<string, Type>();

        foreach (var kvp in _componentIDByType)
        {
            Type type = kvp.Key;
            string name;
            if(!explicitComponentNames.TryGetValue(type, out name!))
                name = type.FullName!;
            
            // Repeated names.
            if(_componentIDByName.ContainsKey(name))
                throw new Exception(
                    $"Component type \"{type.FullName}\" has the same string name (\"{name}\") as another Component type \"{_componentTypes[_componentIDByName[name]]}\".\nConsider using different explicit component name."
                );
            _componentIDByName[name] = kvp.Value;
            _componentNameByType[type] = name!;
            _componentTypeByName[name] = type;
        }
        ComponentTypes = _componentTypes.AsReadOnly();
        ComponentIDByType = _componentIDByType.AsReadOnly();
        ComponentIDByName = _componentIDByName.AsReadOnly();
        ComponentNameByType = _componentNameByType.AsReadOnly();
        ComponentTypeByName = _componentTypeByName.AsReadOnly();
    }

    /// <summary>
    /// Registers a closed generic struct type as a component. If a name is not supplied,
    /// the type's full name is used.
    /// </summary>
    /// <typeparam name="T">A closed generic struct component type.</typeparam>
    /// <param name="name">Optional name used for component lookup and serialization.</param>
    /// <exception cref="ArgumentException">
    /// <typeparamref name="T"/> is not a closed generic value type.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// The component name is already used by another component, or the type was already
    /// registered with a different name.
    /// </exception>
    public static void RegisterComponent<T>(string? name = null) where T : struct
    {
        Type componentType = typeof(T);
        if (!componentType.IsGenericType || componentType.ContainsGenericParameters)
            throw new ArgumentException(
                $"{componentType} must be a closed generic value type.",
                nameof(T)
            );

        string componentName = string.IsNullOrWhiteSpace(name)
            ? componentType.FullName!
            : name;

        lock (_componentRegistrationLock)
        {
            if (_componentIDByType.TryGetValue(componentType, out _))
            {
                if (_componentNameByType[componentType] != componentName)
                    throw new InvalidOperationException(
                        $"Component type \"{componentType}\" is already registered with the name " +
                        $"\"{_componentNameByType[componentType]}\"."
                    );
                return;
            }

            if (_componentIDByName.TryGetValue(componentName, out int existingID))
                throw new InvalidOperationException(
                    $"Component type \"{componentType}\" has the same string name " +
                    $"(\"{componentName}\") as another Component type " +
                    $"\"{_componentTypes[existingID]}\"."
                );

            int componentID = _componentTypes.Count;
            var newPools = new List<(ComponentsManager Manager, IComponentPool Pool)>();
            for (int i = _managers.Count - 1; i >= 0; i--)
            {
                if (_managers[i].TryGetTarget(out ComponentsManager? manager))
                {
                    Type poolType = typeof(ComponentPool<>).MakeGenericType(componentType);
                    var pool = (IComponentPool)Activator.CreateInstance(
                        poolType,
                        manager.OfWorld,
                        manager.MaxEntityCount,
                        4096
                    )!;
                    newPools.Add((manager, pool));
                }
                else
                {
                    _managers.RemoveAt(i);
                }
            }

            _componentTypes.Add(componentType);
            _componentIDByType.Add(componentType, componentID);
            _componentIDByName.Add(componentName, componentID);
            _componentNameByType.Add(componentType, componentName);
            _componentTypeByName.Add(componentName, componentType);

            foreach (var (manager, pool) in newPools)
                manager._pools.Add(pool);
        }
    }

    public static void RegisterComponentAddedDelegate(Type componentType, ComponentAddedDelegate del)
    {
        ComponentAddedDelegates[componentType] = del;
    }
    public static void RegisterComponentAddedDelegate<T>(ComponentAddedDelegate del) where T : struct
    {
        ComponentAddedDelegates[typeof(T)] = del;
    }

    public static void RemoveComponentAddedDelegate(Type componentType)
    {
        ComponentAddedDelegates.Remove(componentType);
    }
    public static void RemoveComponentAddedDelegate<T>() where T : struct
    {
        ComponentAddedDelegates.Remove(typeof(T));
    }

    public static void RegisterComponentRemovedDelegate<T>(ComponentRemovedDelegate del) where T : struct
    {
        ComponentRemovedDelegates[typeof(T)] = del;
    }

    public static void RemoveComponentRemovedDelegate<T>() where T : struct
    {
        ComponentRemovedDelegates.Remove(typeof(T));
    }

    public ComponentsManager(World world, int maxEntityCount)
    {
        OfWorld = world;
        MaxEntityCount = maxEntityCount;
        _pools = new List<IComponentPool>(_componentTypes.Count);
        lock (_componentRegistrationLock)
        {
            SetupComponentPools();
            _managers.Add(new WeakReference<ComponentsManager>(this));
        }
    }

    // [MemberNotNull(nameof(_pools))]
    private void SetupComponentPools()
    {
        for(int i = 0; i < _componentTypes.Count; i++)
        {
            Type poolType = typeof(ComponentPool<>).MakeGenericType(_componentTypes[i]);
            // Console.WriteLine($"Attempting to construct Pool Type {poolType} of Component Type {_componentTypes[i]}");

            _pools.Add((IComponentPool)Activator.CreateInstance(
                poolType,
                OfWorld,
                MaxEntityCount,
                4096
            )!);
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

    // public T Get<T>(Entity entity) where T : struct
    // {
    //     return GetSet<T>().GetComponent(entity);
    // }

    public ref T Get<T>(Entity entity) where T : struct
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
        Add<T>(entity, default);
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
        int cid = _componentIDByType[typeof(T)];
        return (ComponentPool<T>)_pools[cid];
    }
    public IComponentPool GetSet(int componentID)
    {
        return _pools[componentID];
    }
    public IComponentPool GetSet(Type type)
    {
        return _pools[_componentIDByType[type]];
    }
}
