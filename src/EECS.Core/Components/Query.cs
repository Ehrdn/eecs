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

/// <summary>
/// Returns an IEnumerable that iterates through all the entities with the Components of .With, and without .Without.
/// You can save Query instance using .AsSaved() for better performance.
/// Do not call any method that modify the Query instance while iterating entities through Execute().
/// </summary>
public class Query
{
    public readonly World OfWorld;
    private readonly ComponentsManager _components;
    
    private readonly List<int> _withTypes;
    private readonly List<int> _withoutTypes;
    private readonly List<IComponentPool> _withPools;
    private readonly List<IComponentPool> _withoutPools;
    
    /// <summary>
    /// Whether or not this Query instance is saved and reused.
    /// Only affects optimization.
    /// If true, this instance will update its minimum entity pool everytime its Execute() is called.
    /// You can also use MinPoolUpdated() to trigger a min entity pool update.
    /// </summary>
    public bool IsSaved { get; private set; }
    /// <summary>
    /// Updated at every .With(), and will update at Execute() if IsSaved is true.
    /// </summary>
    private int _minPoolIndex;

    public Query(World world)
    {
        _withTypes = [];
        _withoutTypes = [];
        OfWorld = world;
        _components = OfWorld.Components;
        _withPools = [];
        _withoutPools = [];
        IsSaved = false;
        _minPoolIndex = 0;
    }
    
    public Query() : this(World.DefaultWorld!) { }

    public Query AsSaved()
    {
        IsSaved = true;
        return this;
    }

    /// <summary>
    /// Modify the Query instance to make entities require having Component of Type T to be iterated by the Query.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns>This instance.</returns>
    public Query With<T>() where T : struct
    {
        int compID = ComponentsManager.ComponentTypeIDs[typeof(T)];
        _withTypes.Add(compID);
        _withPools.Add(_components.GetSet(compID));

        int minPoolCount = _withPools[_minPoolIndex].Count;
        int curPoolCount = _withPools[_withPools.Count - 1].Count;
        _minPoolIndex = curPoolCount < minPoolCount ? _withPools.Count - 1 : _minPoolIndex;
        
        return this;
    }

    /// <summary>
    /// Modify the Query instance to exclude entity with Component of Type T from the Query.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns>This instance.</returns>
    public Query Without<T>() where T : struct
    {
        int compID = ComponentsManager.ComponentTypeIDs[typeof(T)];
        _withoutTypes.Add(compID);
        _withoutPools.Add(_components.GetSet(compID));
        return this;
    }

    /// <summary>
    /// Modify the Query to notify the ECS that during the iteration of this Query, information of Component Type T might be accessed.
    /// Not implemented. Some future feature may require writing this.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns>This instance.</returns>
    public Query Optional<T>() where T : struct
    {
        return this;
    }

    private void UpdateMinPoolIndex()
    {
        for(int i = 0; i < _withTypes.Count; i++)
        {
            int minPoolCount = _withPools[_minPoolIndex].Count;
            int curPoolCount = _withPools[i].Count;
            _minPoolIndex = curPoolCount < minPoolCount ? i : _minPoolIndex;
        }
    }
    public Query MinPoolUpdated()
    {
        UpdateMinPoolIndex();
        return this;
    }

    public IEnumerable<Entity> Execute()
    {
        if(_withPools.Count <= 0)
            throw new InvalidOperationException(
                "Query requires at least one component added by .With() before executing."
            );
        
        if(IsSaved)
            UpdateMinPoolIndex();
        
        foreach(Entity entity in _withPools[_minPoolIndex].GetEntities())
        {
            bool entityQualify = true;

            for(int i = 0; i < _withTypes.Count; i++)
            {
                if(!_withPools[i].EntityHasComponent(entity))
                {
                    entityQualify = false;
                    break;
                }
            }
            if(!entityQualify) continue;

            for(int i = 0; i < _withoutTypes.Count; i++)
            {
                if(_withoutPools[i].EntityHasComponent(entity))
                {
                    entityQualify = false;
                    break;
                }
            }
            if(!entityQualify) continue;

            yield return entity;
        }
    }

    /// <summary>
    /// Use this when needing to Add or Remove related components when iterating.
    /// </summary>
    /// <returns></returns>
    public List<Entity> ExecuteSnapshot()
    {
        return Execute().ToList();
    }
}
