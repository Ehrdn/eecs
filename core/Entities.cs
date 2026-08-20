using System;
using System.Collections.Generic;
using System.Dynamic;

namespace EECS;

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
public class EntitiesManager
{
    public readonly World OfWorld;
    /// <summary>
    /// The max entity ID reached + 1, remains even after entities are Destroyed. Only decreased when resets.
    /// </summary>
    public int EntityIDCount { get; private set; } = 0;

    private Dictionary<int, int> _generations;
    private Queue<int> _availableIDs;

    public Entity CreateEntity()
    {
        int id;
        if(_availableIDs.Count != 0)
            id = _availableIDs.Dequeue();
        else
            id = EntityIDCount++;
        int gen = GetEntityGeneration(id);
        return new Entity(id, gen);
    }
    // TODO: Make all components that the entity has removed from their sparse set.
    public void DestroyEntity(Entity entity)
    {
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

    public void Reset()
    {
        EntityIDCount = 0;
        _generations.Clear();
        _availableIDs.Clear();
    }

    /// <summary>
    /// Registering that an entity has a component now.
    /// Only expected to be called by SparseSet.
    /// </summary>
    /// <param name="entity"></param>
    /// <param name="componentID"></param>
    internal void RegisterEntityComponentAdd(Entity entity, int componentID)
    {
        
    }

    public EntitiesManager(World world)
    {
        OfWorld = world;
        _generations = new Dictionary<int, int>();
        _availableIDs = new Queue<int>();
    }
}
