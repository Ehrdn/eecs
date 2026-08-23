using System;
using System.Reflection;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Collections;

namespace EECS.Core.Components;

public class Query
{
    private readonly Type[] _types;
    private readonly int[] _typeIDs;
    public ReadOnlyCollection<Type> Types;
    public readonly World OfWorld;
    private readonly ComponentsManager _componentsManager;
    private IComponentPool[] _pools;

    public Query(Type[] types) : this(World.DefaultWorld!, types) { }

    public Query(World world, Type[] types)
    {
        OfWorld = world;
        _componentsManager = world.Components;

        _types = new Type[types.Length];
        Types = _types.AsReadOnly();
        _typeIDs = new int[types.Length];
        _pools = new IComponentPool[types.Length];
        for(int i = 0; i < types.Length; i++)
        {
            _types[i] = types[i];
            _typeIDs[i] = ComponentsManager.ComponentTypeIDs[types[i]];
            _pools[i] = _componentsManager.GetSet(_typeIDs[i]);
        }
    }

    internal IEnumerable<Entity> Execute(int minTypeIndex)
    {
        // Type minType = _types[minTypeIndex];
        // int minTypeID = _typeIDs[minTypeIndex];
        
        foreach(Entity entity in _pools[minTypeIndex].GetEntities())
        {
            bool entityHasAllComponents = true;
            for(int i = 0; i < _types.Length; i++)
            {
                if(i == minTypeIndex) continue;
                if(!_pools[i].EntityHasComponent(entity))
                {
                    entityHasAllComponents = false;
                    break;
                }
            }
            if(entityHasAllComponents) yield return entity;
        }
    }
    internal int GetMinEntityCountTypeIndex()
    {
        int minTypeIndex = 0;
        int minCount = _pools[0].Count;
        for(int i = 1; i < _types.Length; i++)
        {
            int entCount = _pools[i].Count;
            minTypeIndex = entCount < minCount ? i : minTypeIndex;
            minCount = entCount < minCount ? entCount : minCount;
        }
        return minTypeIndex;
    }
    public IEnumerable<Entity> Execute()
    {
        int minTypeIndex = GetMinEntityCountTypeIndex();
        return Execute(minTypeIndex);
    }

    /// <summary>
    /// Use this when needing to Add or Remove related components when iterating.
    /// </summary>
    /// <returns></returns>
    public List<Entity> ExecuteSnapshot()
    {
        int minTypeIndex = GetMinEntityCountTypeIndex();
        int minTypeEntityCount = _pools[minTypeIndex].Count;
        List<Entity> list = new List<Entity>();
        foreach(Entity entity in Execute(minTypeIndex))
            list.Add(entity);
        return list;
    }
}
