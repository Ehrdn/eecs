using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Dynamic;
using System.Reflection.Metadata;

using EECS.Core.Components;

namespace EECS.Core;

/// <summary>
/// Entity.
/// Entities belongs to a World, but they don't store it.
/// Who it belongs to depends on the World.
/// </summary>
public readonly struct Entity : IEquatable<Entity>
{
    public readonly ulong Value;

    public int ID => (int)(Value & 0xFFFFFFFF);
    public int Generation => (int)(Value >> 32);

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
    public int EntityIDCount { get; private set; } = 0;
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

    public Entity CreateEntity()
    {
        int id;
        if(_availableIDs.Count != 0)
            id = _availableIDs.Dequeue();
        else
            id = EntityIDCount++;
        
        if(_entityComponents[id] == null)
            _entityComponents[id] = new List<int>();

        int gen = GetEntityGeneration(id);
        return new Entity(id, gen);
    }
    
    public void DestroyEntity(Entity entity)
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
    
    public int GetEntityGeneration(int id)
    {
        if(!_generations.ContainsKey(id))
            return 0;
        else
            return _generations[id];
    }

    public bool IsAlive(Entity entity)
    {
        return entity.ID >= 0
            && entity.ID < EntityIDCount
            && GetEntityGeneration(entity.ID) == entity.Generation;
    }

    /// <summary>
    /// Should be called along with ComponentsManager's Reset().
    /// </summary>
    internal void Reset()
    {
        EntityIDCount = 0;
        _generations.Clear();
        _availableIDs.Clear();
        _entityComponents = new List<int>[MaxEntityID + 1];
    }

    /// <summary>
    /// It's like... Dispose(), but internal.
    /// </summary>
    internal void Free()
    {
        _generations = null!;
        _availableIDs = null!;
        _entityComponents = null!;
    }

    /// <summary>
    /// Registering that an entity has just been added a component.
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
    /// Registering that an entity has just been removed a component.
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
