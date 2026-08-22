using System;
using System.Reflection;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.ComponentModel;
using System.Collections.ObjectModel;

namespace EECS.Core.Components;

public interface IComponentPool
{
    /// <summary>
    /// Component Type this pool manages.
    /// </summary>
    Type ComponentType { get; }
    /// <summary>
    /// Count of Entities that have this component.
    /// </summary>
    int Count { get; }

    bool EntityHasComponent(Entity entity);
    void RemoveFromEntity(Entity entity);
    void AddToEntity(Entity entity);
    internal void EntityDestoryed(Entity entity);
    internal void Reset();
    internal void Free();
    ReadOnlyCollection<Entity> GetEntities();
}


/// <summary>
/// Paged array of int for Sparse Array inside of ComponentPool.
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

/// <summary>
/// Manages the logic of a single type of Component.
/// </summary>
/// <typeparam name="T">The component type.</typeparam>
public class ComponentPool<T> : IComponentPool where T : struct
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
    ReadOnlyCollection<Entity> IComponentPool.GetEntities() => _entities.AsReadOnly();
    
    /// <summary>
    /// Stores indexes in dense table. If -1, means it doesn't exist.
    /// </summary>
	private PagedArray _sparse;
    /// <summary>
    /// Count of Entities that have this component.
    /// </summary>
    public int Count => _dense.Count;

    public readonly World OfWorld;

    /// <summary>
    /// Entities Manager of this component pool.
    /// Used to validate entity status, and to register component changes on an entity.
    /// </summary>
    private EntitiesManager _entitiesManager;

    public readonly int MaxID;
    public readonly int SparseArrayPageSize;

    public Type ComponentType => typeof(T);
    public int ComponentID => ComponentsManager.ComponentTypeIDs[ComponentType];

	public ComponentPool(World world, int maxID, int sparseArrayPageSize = 4096)
	{
        OfWorld = world;
        _entitiesManager = world.Entities;
        MaxID = maxID;
        SparseArrayPageSize = sparseArrayPageSize;

		_dense = new List<T>();
		_entities = new List<Entity>();
		_sparse = new PagedArray(maxID + 1, sparseArrayPageSize);
	}

    /// <summary>
    /// Get dense index, and make sure that the entity is alive and has the component.
    /// </summary>
    /// <param name="entity"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    /// <exception cref="InvalidOperationException"></exception>
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
        
        int denseIndex = _sparse[entity.ID];
        _dense.Add(comp);
        _entities.Add(entity);
        _sparse[entity.ID] = denseIndex;

        _entitiesManager.TrackComponentAdd(entity, ComponentID);
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

        
        _entitiesManager.TrackComponentRemove(entity, ComponentID);
    }

    /// <summary>
    /// Unchecked and untracked remove component from entity.
    /// </summary>
    /// <param name="entity"></param>
    void IComponentPool.EntityDestoryed(Entity entity)
    {
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
    
    void IComponentPool.Reset()
    {
        _dense.Clear();
        _entities.Clear();
        _sparse = new PagedArray(MaxID + 1, SparseArrayPageSize);
    }

    /// <summary>
    /// Dispose(), but internal.
    /// </summary>
    void IComponentPool.Free()
    {
        _dense = null!;
        _sparse = null!;
        _entities = null!;
    }
}
