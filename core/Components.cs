using System;
using System.Reflection;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace EECS;

/// <summary>
/// Attach this to struct that should be components.
/// </summary>
[AttributeUsage(AttributeTargets.Struct, Inherited = false)]
public sealed class ComponentAttribute : System.Attribute
{
    public ComponentAttribute() { }
}

public interface ISparseSet
{
    Type ComponentType { get; }
    int Count { get; }

    bool EntityHasComponent(Entity entity);
    void RemoveFromEntity(Entity entity);
    void AddToEntity(Entity entity);
    void Reset();
}


/// <summary>
/// Paged array of int for Sparse Array inside of SparseSet.
/// </summary>
internal class PagedArray
{
    public readonly int PageCount;
    public readonly int PageSize;
    public readonly int Length;
    public int Capacity => PageCount * PageSize;
    private int[][] _arr;

    public PagedArray(int length, int pageSize)
    {
        if(length < 0)
            throw new ArgumentOutOfRangeException(nameof(length));
        if(pageSize <= 0)
            throw new ArgumentOutOfRangeException(nameof(pageSize));
        PageCount = (length + pageSize - 1) / pageSize;
        PageSize = pageSize;
        Length = length;
        _arr = new int[PageCount][];
    }

    public int this[int k]
    {
        get
        {
            int[] page = _arr[k / PageSize];
            if(page == null)
                return -1;
            return page[k % PageSize];
        }
        set
        {
            int pageIndex = k / PageSize;
            int slotIndex = k % PageSize;
            int[] page = _arr[pageIndex];
            if(page == null)
            {
                page = new int[PageSize];
                Array.Fill(page, -1);
                _arr[pageIndex] = page;
            }
            page[slotIndex] = value;
        }
    }
}

public class SparseSet<T> : ISparseSet where T : struct
{
    /// <summary>
    /// Dense array for component. Starts from 0.
    /// </summary>
	private List<T> _dense;
	/// <summary>
    /// Dense array for entities, corresponds to dense[].
    /// Also Starts from 0.
    /// </summary>
    private List<Entity> _entities;
    /// <summary>
    /// Stores indexes in dense table. If -1, means it doesn't exist.
    /// </summary>
	private PagedArray _sparse;
    /// <summary>
    /// Count of Entities that have this component.
    /// </summary>
    public int Count => _dense.Count;

    /// <summary>
    /// Entities Manager of this sparse set.
    /// Used to validate entity status, and to register component changes on an entity.
    /// </summary>
    private EntitiesManager _entitiesManager;

    public readonly int MaxID;
    public readonly int SparseArrayPageSize;

	public SparseSet(EntitiesManager entitiesManager, int maxID, int sparseArrayPageSize = 4096)
	{
        _entitiesManager = entitiesManager;
        MaxID = maxID;
        SparseArrayPageSize = sparseArrayPageSize;

		_dense = new List<T>();
		_entities = new List<Entity>();
		_sparse = new PagedArray(maxID, sparseArrayPageSize);
	}

    public Type ComponentType => typeof(T);

    private int GetDenseIndex(Entity entity)
    {
        if (!_entitiesManager.IsAlive(entity))
            throw new ArgumentException(entity + $" is not alive.");

        int index = _sparse[entity.ID];

        if (index < 0 || _entities[index] != entity)
            throw new InvalidOperationException(
                entity + $" doesn't have component {typeof(T).FullName}."
            );

        return index;
    }

    public void AddToEntity(Entity entity, T comp)
    {
        if(EntityHasComponent(entity))
            throw new InvalidOperationException(
                entity + $" already has component {typeof(T).FullName}."
            );
        
        int denseIndex = GetDenseIndex(entity);
        _dense.Add(comp);
        _entities.Add(entity);
        _sparse[entity.ID] = denseIndex;
    }
    public void AddToEntity(Entity entity)
    {
        AddToEntity(entity, new T());
    }

    public T GetComponent(Entity entity)
    {
        return _dense[GetDenseIndex(entity)];
    }

    public void SetComponent(Entity entity, T component)
    {
        _dense[GetDenseIndex(entity)] = component;
    }

    /// <summary>
    /// Get or set the resoecutve entity's component.
    /// The entity must have the component first. Use AddToEntity() to add a component to an entity, and RemoveFromEntity() to remove it.
    /// </summary>
    /// <param name="entity"></param>
    /// <returns></returns>
    public T this[Entity entity]
    {
        get => GetComponent(entity);
        set => SetComponent(entity, value);
    }

    public void RemoveFromEntity(Entity entity)
    {
        if(!EntityHasComponent(entity))
            throw new InvalidOperationException(
                entity + $" doesn't have component {typeof(T).FullName}."
            );
        if(!_entitiesManager.IsAlive(entity))
            throw new ArgumentException(entity + "is not alive."); 

        int denseIndex = _sparse[entity.ID];
        int lastIndex = _dense.Count - 1;

        if(denseIndex != lastIndex)
        {
            Entity movedEntity = _entities[lastIndex];
            _dense[denseIndex] = _dense[lastIndex];
            _entities[denseIndex] = movedEntity;
            _sparse[movedEntity.ID] = denseIndex;
        }

        _dense.RemoveAt(lastIndex);
        _entities.RemoveAt(lastIndex);
        _sparse[entity.ID] = -1;
    }

    public bool EntityHasComponent(Entity entity)
    {
        if (!_entitiesManager.IsAlive(entity))
            return false;

        int index = _sparse[entity.ID];

        return index >= 0 && _entities[index] == entity;
    }
    
    public void Reset()
    {
        _dense.Clear();
        _entities.Clear();
        _sparse = new PagedArray(MaxID, SparseArrayPageSize);
    }
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

    private ISparseSet[] _sets;

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
        int i = 0;
        foreach(var t in componentTypes)
        {
            _componentTypes[i] = t.Type;
            _componentTypeIDs[t.Type] = i++;
        }
    }

    public ComponentsManager(World world, int maxEntityID)
    {
        OfWorld = world;
        MaxEntityID = maxEntityID;
        SetupSparseSets();
    }

    [MemberNotNull(nameof(_sets))]
    private void SetupSparseSets()
    {
        _sets = new ISparseSet[_componentTypes.Length];
        for(int i = 0; i < _componentTypes.Length; i++)
        {
            Type setType = typeof(SparseSet<>).MakeGenericType(_componentTypes[i]);

            _sets[i] = (ISparseSet)Activator.CreateInstance(
                setType,
                OfWorld.Entities,
                MaxEntityID
            )!;
        }
    }

    public void Reset()
    {
        foreach (var set in _sets)
            set.Reset();
    }

    public T Get<T>(Entity entity) where T : struct
    {
        return GetSet<T>()[entity];
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
    /// Alternative API to get the Sparse Set and manipulate components from there.
    /// And also the Sparse Set lets you Get and Set the component from it by using componentsManager.GetSet<ComponentType>()[entity].
    /// ...Does this count as a pun?
    /// </summary>
    /// <typeparam name="T">Type of the component.</typeparam>
    /// <returns></returns>
    public SparseSet<T> GetSet<T>() where T : struct
    {
        int cid = _componentTypeIDs[typeof(T)];
        return ((SparseSet<T>)_sets[cid]);
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

public class Query
{
    public Query(Type[] types)
    {
        
    }
}
