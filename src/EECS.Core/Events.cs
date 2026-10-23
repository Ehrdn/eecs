using System.Reflection.Metadata;
using EECS.Core.Components;

namespace EECS.Core;

/// <summary>
/// Instance of this class describes a condition a entity might or might not satisfy.
/// </summary>
public class EntityCondition
{
    public readonly World OfWorld;
    public readonly ComponentsManager _components;
    private readonly List<int> _withTypes;
    private readonly List<int> _withoutTypes;

    private readonly List<IComponentPool> _withPools;
    private readonly List<IComponentPool> _withoutPools;

    public Predicate<Entity>? ConditionPredicate;

    public EntityCondition(World world, Predicate<Entity>? conditionPredicate = null)
    {
        OfWorld = world;
        _components = world.Components;
        
        _withTypes = new();
        _withoutTypes = new();
        
        _withPools = new();
        _withoutPools = new();

        ConditionPredicate = conditionPredicate;
    }

    public bool Satisfied(Entity entity)
    {
        for(int i = 0; i < _withTypes.Count; i++)
            if(!_withPools[i].EntityHasComponent(entity))
                return false;

        for(int i = 0; i < _withoutTypes.Count; i++)
            if(_withoutPools[i].EntityHasComponent(entity))
                return false;

        if(ConditionPredicate == null
        || ConditionPredicate(entity))
            return true;
        
        return false;
    }

    public EntityCondition With(Type componentType)
    {
        int compID = ComponentsManager.ComponentIDByType[componentType];
        _withTypes.Add(compID);
        _withPools.Add(_components.GetSet(compID));

        return this;
    }

    public EntityCondition With<T>()
    {
        return With(typeof(T));
    }
    
    public EntityCondition Without(Type componentType)
    {
        int compID = ComponentsManager.ComponentIDByType[componentType];
        _withoutTypes.Add(compID);
        _withoutPools.Add(_components.GetSet(compID));

        return this;
    }

    public EntityCondition Without<T>()
    {
        return Without(typeof(T));
    }
}

/// <summary>
/// Events that are fired per entity basis.
/// Each instance of this class represents one kind of EntityEvent.
/// </summary>
/// <typeparam name="OrderEnum">A Enum Type of all the orders this event has. The enum type's underlying type must be int (the default). The handlers will be fired by the order of the Enum they're attached to.</typeparam>
/// <typeparam name="EventParam">A struct that will be passed down to handlers, where the previous handlers can modify this struct and pass it to later handlers.</typeparam>
public class EntityEvent<OrderEnum, EventParam>
    where OrderEnum : Enum
    where EventParam : struct
{
    public readonly int OrderCount;
    public delegate ref EventParam EntityEventHandler(Entity entity, ref EventParam evParams);
    
    /// <summary>
    /// _handlers[order][sequence][sameSequenceEventIndex]
    /// </summary>
    private readonly SortedDictionary<int, List<Tuple<EntityCondition?, EntityEventHandler>>>[] _handlers;

    /// <summary>
    /// This is the condition an entity must satisfy in order for the EntityEvent to be fired upon it.
    /// This applies to all entities, to filter out entities per handler basis, use the filter parameter in <see cref="RegisterHandler"/>
    /// </summary>
    public EntityCondition? EventFilter;

    public EntityEvent(EntityCondition? eventFilter = null)
    {
        OrderCount = Enum.GetNames(typeof(OrderEnum)).Length;
        _handlers = new SortedDictionary<int, List<Tuple<EntityCondition?, EntityEventHandler>>>[OrderCount];
        for(int i = 0; i < OrderCount; i++)
            _handlers[i] = new();
        EventFilter = eventFilter;
    }

    // O(n).
    public void RegisterHandler(OrderEnum eventOrder, int orderSequence, EntityEventHandler handler, EntityCondition? filter = null)
    {
        int order = eventOrder.GetHashCode();
        if(!_handlers[order].ContainsKey(orderSequence))
            _handlers[order].Add(orderSequence, new List<Tuple<EntityCondition?, EntityEventHandler>>());
        _handlers[order][orderSequence].Add(new Tuple<EntityCondition?, EntityEventHandler>(filter,handler));
    }

    public void Fire(Entity entity, ref EventParam evParam)
    {
        if(EventFilter != null && !EventFilter.Satisfied(entity))
            return;
        for(int order = 0; order < _handlers.Length; order++)
        {
            foreach(var kv in _handlers[order])
            {
                
            }
        }
    }
}

public class Event<OrderEnum> where OrderEnum : Enum
{
    
}

