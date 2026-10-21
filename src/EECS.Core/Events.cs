
using System.Diagnostics;

namespace EECS.Core;

/// <summary>
/// Instance of this class describes a condition a entity might or might not satisfy/
/// </summary>
public class EntityCondition
{
    public readonly World OfWorld;
    private readonly List<Type> _withTypes;
    private readonly List<Type> _withoutTypes;
    private Predicate<Entity>? _conditionPredicate;

    public EntityCondition(World world, Predicate<Entity>? conditionPredicate = null)
    {
        OfWorld = world;
        _withTypes = new();
        _withoutTypes = new();
        _conditionPredicate = conditionPredicate;
    }

    public bool EntitySatisfyCondition(Entity entity)
    {
        // for(int i = 0; i < _withTypes)
        return true;
    }
}

public class ECSEvent<OrderEnum> where OrderEnum : Enum
{
    public readonly int OrderCount;
    /// <summary>
    /// All delegate in this should have the same param list.
    /// </summary>
    public List<Delegate>[] Handlers;
    
    public struct Delegate
    {
        
    }

    public ECSEvent()
    {
        OrderCount = Enum.GetNames(typeof(OrderEnum)).Length;
        Handlers = new List<Delegate>[OrderCount];
        for(int i = 0; i < OrderCount; i++)
            Handlers[i] = new();
        
    }

    public void Fire()
    {
        
    }
}

