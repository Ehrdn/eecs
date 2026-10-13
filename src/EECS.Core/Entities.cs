using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Dynamic;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;

using EECS.Core.Components;

namespace EECS.Core;

/// <summary>
/// Entity.
/// Entities belongs to a World, but they don't store the world they belong.
/// User should make sure the Entities come from a World only be used in that World.
/// </summary>
public readonly struct Entity : IEquatable<Entity>
{
    public readonly ulong Value;

    public int ID => (int)(Value & 0xFFFFFFFF);
    public int Generation => (int)(Value >> 32);
    /// <summary>
    /// True if the entity's ID is not 0. This doesn not give indication of whether the entity is alive or not, use EntitiesManager.IsAlive instead.
    /// Treat the entity as if it is null if this is false.
    /// </summary>
    public bool IsValid => ID != 0;

    public Entity(int id, int generation)
    {
        Value = ((ulong)(uint)generation << 32) | (uint)id;
    }

    public bool Equals(Entity other)
    {
        return Value == other.Value;
    }

    public override bool Equals(object? obj)
    {
        return obj is Entity other && Equals(other);
    }

    public override int GetHashCode()
    {
        return Value.GetHashCode();
    }

    public static bool operator ==(Entity a, Entity b)
    {
        return a.Value == b.Value;
    }

    public static bool operator !=(Entity a, Entity b)
    {
        return a.Value != b.Value;
    }

    public override string ToString()
    {
        return $"Entity {ID} (generation {Generation})";
    }
}

#region EntityBuffers

[InlineArray(8)]
public struct Entity8
{
    private Entity _value;
}

public struct EntityBuffer8 : IList<Entity>, IReadOnlyList<Entity>
{
    public Entity8 Entities;
    public byte Count;

    public Entity this[int index]
    {
        get
        {
            if ((uint)index >= Count)
                throw new ArgumentOutOfRangeException(nameof(index));
            return Entities[index];
        }
        set
        {
            if ((uint)index >= Count)
                throw new ArgumentOutOfRangeException(nameof(index));
            Entities[index] = value;
        }
    }

    int ICollection<Entity>.Count => Count;
    int IReadOnlyCollection<Entity>.Count => Count;
    bool ICollection<Entity>.IsReadOnly => false;

    public void Add(Entity entity)
    {
        if (Count == 8)
            throw new InvalidOperationException("The entity buffer is full.");
        Entities[Count++] = entity;
    }

    public void Clear()
    {
        for (int i = 0; i < Count; i++)
            Entities[i] = default;
        Count = 0;
    }

    public bool Contains(Entity entity) => IndexOf(entity) >= 0;

    public int IndexOf(Entity entity)
    {
        for (int i = 0; i < Count; i++)
            if (Entities[i] == entity)
                return i;
        return -1;
    }

    public void CopyTo(Entity[] array, int arrayIndex)
    {
        if (array == null)
            throw new ArgumentNullException(nameof(array));
        if (arrayIndex < 0 || arrayIndex > array.Length || array.Length - arrayIndex < Count)
            throw new ArgumentOutOfRangeException(nameof(arrayIndex));

        for (int i = 0; i < Count; i++)
            array[arrayIndex + i] = Entities[i];
    }

    public bool Remove(Entity entity)
    {
        int index = IndexOf(entity);
        if (index < 0)
            return false;
        RemoveAt(index);
        return true;
    }

    public void Insert(int index, Entity entity)
    {
        if ((uint)index > Count)
            throw new ArgumentOutOfRangeException(nameof(index));
        if (Count == 8)
            throw new InvalidOperationException("The entity buffer is full.");

        for (int i = Count; i > index; i--)
            Entities[i] = Entities[i - 1];
        Entities[index] = entity;
        Count++;
    }

    public void RemoveAt(int index)
    {
        if ((uint)index >= Count)
            throw new ArgumentOutOfRangeException(nameof(index));

        for (int i = index; i < Count - 1; i++)
            Entities[i] = Entities[i + 1];
        Entities[--Count] = default;
    }

    public IEnumerator<Entity> GetEnumerator()
    {
        for (int i = 0; i < Count; i++)
            yield return Entities[i];
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

[InlineArray(16)]
public struct Entity16
{
    Entity _value;
}

public struct EntityBuffer16 : IList<Entity>, IReadOnlyList<Entity>
{
    public Entity16 Entities;
    public byte Count;

    public Entity this[int index]
    {
        get
        {
            if ((uint)index >= Count)
                throw new ArgumentOutOfRangeException(nameof(index));
            return Entities[index];
        }
        set
        {
            if ((uint)index >= Count)
                throw new ArgumentOutOfRangeException(nameof(index));
            Entities[index] = value;
        }
    }

    int ICollection<Entity>.Count => Count;
    int IReadOnlyCollection<Entity>.Count => Count;
    bool ICollection<Entity>.IsReadOnly => false;

    public void Add(Entity entity)
    {
        if (Count == 16)
            throw new InvalidOperationException("The entity buffer is full.");
        Entities[Count++] = entity;
    }

    public void Clear()
    {
        for (int i = 0; i < Count; i++)
            Entities[i] = default;
        Count = 0;
    }

    public readonly bool Contains(Entity entity) => IndexOf(entity) >= 0;

    public readonly int IndexOf(Entity entity)
    {
        for (int i = 0; i < Count; i++)
            if (Entities[i] == entity)
                return i;
        return -1;
    }

    public readonly void CopyTo(Entity[] array, int arrayIndex)
    {
        if (array == null)
            throw new ArgumentNullException(nameof(array));
        if (arrayIndex < 0 || arrayIndex > array.Length || array.Length - arrayIndex < Count)
            throw new ArgumentOutOfRangeException(nameof(arrayIndex));

        for (int i = 0; i < Count; i++)
            array[arrayIndex + i] = Entities[i];
    }

    public bool Remove(Entity entity)
    {
        int index = IndexOf(entity);
        if (index < 0)
            return false;
        RemoveAt(index);
        return true;
    }

    public void Insert(int index, Entity entity)
    {
        if ((uint)index > Count)
            throw new ArgumentOutOfRangeException(nameof(index));
        if (Count == 16)
            throw new InvalidOperationException("The entity buffer is full.");

        for (int i = Count; i > index; i--)
            Entities[i] = Entities[i - 1];
        Entities[index] = entity;
        Count++;
    }

    public void RemoveAt(int index)
    {
        if ((uint)index >= Count)
            throw new ArgumentOutOfRangeException(nameof(index));

        for (int i = index; i < Count - 1; i++)
            Entities[i] = Entities[i + 1];
        Entities[--Count] = default;
    }

    public IEnumerator<Entity> GetEnumerator()
    {
        for (int i = 0; i < Count; i++)
            yield return Entities[i];
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}


[InlineArray(32)]
public struct Entity32
{
    Entity _value;
}

public struct EntityBuffer32 : IList<Entity>, IReadOnlyList<Entity>
{
    public Entity32 Entities;
    public byte Count;

    public Entity this[int index]
    {
        get
        {
            if ((uint)index >= Count)
                throw new ArgumentOutOfRangeException(nameof(index));
            return Entities[index];
        }
        set
        {
            if ((uint)index >= Count)
                throw new ArgumentOutOfRangeException(nameof(index));
            Entities[index] = value;
        }
    }

    int ICollection<Entity>.Count => Count;
    int IReadOnlyCollection<Entity>.Count => Count;
    bool ICollection<Entity>.IsReadOnly => false;

    public void Add(Entity entity)
    {
        if (Count == 32)
            throw new InvalidOperationException("The entity buffer is full.");
        Entities[Count++] = entity;
    }

    public void Clear()
    {
        for (int i = 0; i < Count; i++)
            Entities[i] = default;
        Count = 0;
    }

    public bool Contains(Entity entity) => IndexOf(entity) >= 0;

    public int IndexOf(Entity entity)
    {
        for (int i = 0; i < Count; i++)
            if (Entities[i] == entity)
                return i;
        return -1;
    }

    public void CopyTo(Entity[] array, int arrayIndex)
    {
        if (array == null)
            throw new ArgumentNullException(nameof(array));
        if (arrayIndex < 0 || arrayIndex > array.Length || array.Length - arrayIndex < Count)
            throw new ArgumentOutOfRangeException(nameof(arrayIndex));

        for (int i = 0; i < Count; i++)
            array[arrayIndex + i] = Entities[i];
    }

    public bool Remove(Entity entity)
    {
        int index = IndexOf(entity);
        if (index < 0)
            return false;
        RemoveAt(index);
        return true;
    }

    public void Insert(int index, Entity entity)
    {
        if ((uint)index > Count)
            throw new ArgumentOutOfRangeException(nameof(index));
        if (Count == 32)
            throw new InvalidOperationException("The entity buffer is full.");

        for (int i = Count; i > index; i--)
            Entities[i] = Entities[i - 1];
        Entities[index] = entity;
        Count++;
    }

    public void RemoveAt(int index)
    {
        if ((uint)index >= Count)
            throw new ArgumentOutOfRangeException(nameof(index));

        for (int i = index; i < Count - 1; i++)
            Entities[i] = Entities[i + 1];
        Entities[--Count] = default;
    }

    public IEnumerator<Entity> GetEnumerator()
    {
        for (int i = 0; i < Count; i++)
            yield return Entities[i];
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}


#endregion

/// <summary>
/// Instance belongs to a World.
/// </summary>
public partial class EntitiesManager
{
    public readonly World OfWorld;
    private ComponentsManager _componentsManager => OfWorld.Components;
    /// <summary>
    /// The max entity ID reached + 1, remains even after entities are Destroyed. Only decreased when resets.
    /// </summary>
    public int EntityIDCount { get; private set; } = 1;
    public readonly int MaxEntityID;

    private Dictionary<int, int> _generations;
    private Queue<int> _availableIDs;
    /// <summary>
    /// Stores Component ID List of the entity.
    /// Makes destroying entities O(k) and not O(C), where k is the number of components on that entity and C is the total components types count.
    /// Remove a component from a single entity is O(k) because of this but still pretty cheap.
    /// Cheaper than Archetype ECS, I hope......
    /// </summary>
    private List<int>[] _entityComponents;

    public IReadOnlyList<int>? GetEntityComponentIDs(Entity entity)
    {
        if(!IsAlive(entity))
            return null;
        return _entityComponents[entity.ID]?.AsReadOnly();
    }

    public Entity Create()
    {
        int id;
        if(_availableIDs.Count != 0)
            id = _availableIDs.Dequeue();
        else
            id = EntityIDCount++;
        
        if(_entityComponents[id] == null)
            _entityComponents[id] = new List<int>();

        int gen = GetGeneration(id);
        return new Entity(id, gen);
    }
    
    public void Destroy(Entity entity)
    {
        if (!IsAlive(entity))
            throw new ArgumentException($"{entity} is not alive.");
        
        // Remove all components of the entity from component pools.
        foreach(int cid in _entityComponents[entity.ID])
            _componentsManager.GetSet(cid).EntityDestroyed(entity);
        _entityComponents[entity.ID].Clear();

        if(!_generations.ContainsKey(entity.ID))
            _generations[entity.ID] = 1;
        else
            _generations[entity.ID]++;
        _availableIDs.Enqueue(entity.ID);
    }
    
    public int GetGeneration(int entityID)
    {
        if(!_generations.ContainsKey(entityID))
            return 0;
        else
            return _generations[entityID];
    }

    public bool IsAlive(Entity entity)
    {
        return entity.IsValid
            && entity.ID < EntityIDCount
            && GetGeneration(entity.ID) == entity.Generation;
    }

    /// <summary>
    /// Should be called along with ComponentsManager's Reset().
    /// </summary>
    internal void Reset()
    {
        EntityIDCount = 1;
        _generations.Clear();
        _availableIDs.Clear();
        _entityComponents = new List<int>[MaxEntityID + 1];
    }

    /// <summary>
    /// Registering that an entity has just added a component.
    /// Only expected to be called by ComponentPool.
    /// </summary>
    /// <param name="entity"></param>
    /// <param name="componentID"></param>
    internal void TrackComponentAdd(Entity entity, int componentID)
    {
        int eid = entity.ID;
        
        // ComponentPool should have already checked if the entity is alive by this point.
        
        _entityComponents[eid].Add(componentID);
    }
    
    /// <summary>
    /// Registering that an entity has just removed a component.
    /// Only expected to be called by ComponentPool.
    /// </summary>
    /// <param name="entity"></param>
    /// <param name="componentID"></param>
    internal void TrackComponentRemove(Entity entity, int componentID)
    {
        int eid = entity.ID;
        
        // ComponentPool should have already checked if the entity is alive by this point.
        
        _entityComponents[eid].Remove(componentID);
    }

    public EntitiesManager(World world, int maxEntityID)
    {
        OfWorld = world;
        MaxEntityID = maxEntityID;
        _generations = new Dictionary<int, int>();
        _availableIDs = new Queue<int>();
        _entityComponents = new List<int>[MaxEntityID + 1];
    }
}
