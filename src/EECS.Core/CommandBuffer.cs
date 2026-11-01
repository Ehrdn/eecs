using System.Collections;
using System.Data;
using System.Data.SqlTypes;
using System.Xml;
using EECS.Core;
using EECS.Core.Components;

namespace EECS.Core;

internal enum ECSCommandTypes
{
    Add,
    Remove,
    Set,
    Create, // Entities
    Destroy,
}

internal readonly struct ECSCommand
{
    public readonly World? PrototypeWorld;
    public readonly bool TargetsFuture = false;
    public readonly Entity Target;
    public readonly FutureEntity FutureTarget;
    public readonly Entity Prototype;
    public readonly Type? ComponentType;
    public readonly ECSCommandTypes CommandType;
    
    public readonly object? ComponentSetTo;
    
    public ECSCommand(Entity entity, ECSCommandTypes commandType, Type? componentType = null, object? setTo = null)
    {
        TargetsFuture = false;
        Target = entity;
        ComponentType = componentType;
        CommandType = commandType;
        ComponentSetTo = setTo;
    }
    public ECSCommand(FutureEntity futureEntity, World? prototypeWorld = null, Entity prototype = default)
    {
        TargetsFuture = true;
        FutureTarget = futureEntity;
        PrototypeWorld = prototypeWorld;
        Prototype = prototype;
    }
    public ECSCommand(FutureEntity entity, ECSCommandTypes commandType, Type? componentType = null, object? setTo = null)
    {
        TargetsFuture = true;
        FutureTarget = entity;
        ComponentType = componentType;
        CommandType = commandType;
        ComponentSetTo = setTo;
    }
}


/// <summary>
/// Reference to an entity that is set to be created by a command buffer.
/// This will be invalid once the command buffer executes or clears.
/// Do not create this manually, the default value of this is will point to an invalid entity (id = 0).
/// </summary>
public struct FutureEntity
{
    internal int Index;
    internal FutureEntity(int index)
    {
        Index = index;
    }
}

public class ECSCommandBuffer<T>
{
    
}

public class ECSCommandBuffer
{
    public readonly World OfWorld;
    private ComponentsManager _components;
    private List<ECSCommand> _commands;

    public ECSCommandBuffer(World ofWorld)
    {
        OfWorld = ofWorld;
        _components = OfWorld.Components;
        _commands = new();
        _futureEntities = [default];
    }

    /// <summary>
    /// Index of a FutureEntityRef corresponds to this.
    /// </summary>
    private readonly List<Entity> _futureEntities;

    public void AddComponent(Entity entity, Type componentType)
    {
        _commands.Add(new ECSCommand(entity, ECSCommandTypes.Add, componentType));
    }
    public void AddComponent<T>(Entity entity)
    {
        AddComponent(entity, typeof(T));
    }

    public void AddComponent(FutureEntity entity, Type componentType)
    {
        _commands.Add(new ECSCommand(entity, ECSCommandTypes.Add, componentType));
    }
    public void AddComponent<T>(FutureEntity entity)
    {
        AddComponent(entity, typeof(T));
    }
    
    public void RemoveComponent(Entity entity, Type componentType)
    {
        _commands.Add(new ECSCommand(entity, ECSCommandTypes.Remove, componentType));
    }

    public void RemoveComponent<T>(Entity entity)
    {
        RemoveComponent(entity, typeof(T));
    }

    public void RemoveComponent(FutureEntity entity, Type componentType)
    {
        _commands.Add(new ECSCommand(entity, ECSCommandTypes.Remove, componentType));
    }

    public void RemoveComponent<T>(FutureEntity entity)
    {
        RemoveComponent(entity, typeof(T));
    }

    public void SetComponent(Entity entity, Type componentType, object setTo)
    {
        _commands.Add(new ECSCommand(entity, ECSCommandTypes.Set, componentType, setTo));
    }
    public void SetComponent<T>(Entity entity, in T setTo)
    {
        _commands.Add(new ECSCommand(entity, ECSCommandTypes.Set, typeof(T), setTo));
    }

    public void SetComponent(FutureEntity entity, Type componentType, object setTo)
    {
        _commands.Add(new ECSCommand(entity, ECSCommandTypes.Set, componentType, setTo));
    }
    public void SetComponent<T>(FutureEntity entity, in T setTo)
    {
        _commands.Add(new ECSCommand(entity, ECSCommandTypes.Set, typeof(T), setTo));
    }

    /// <summary>
    /// Intended to be used in conjuction of components manipulation.
    /// This is slower than pure batch entity creation.
    /// Just do it yourself with a simple loop if you just want a efficient batch entity creation.
    /// </summary>
    /// <returns></returns>
    public FutureEntity CreateEntity()
    {
        _futureEntities.Add(default);
        var futureEntity = new FutureEntity(_futureEntities.Count - 1);
        _commands.Add(new ECSCommand(futureEntity));
        return futureEntity;
    }
    
    public FutureEntity CreateEntity(World prototypeWorld, Entity prototype)
    {
        _futureEntities.Add(default);
        var futureEntity = new FutureEntity(_futureEntities.Count - 1);
        _commands.Add(new ECSCommand(futureEntity, prototypeWorld, prototype));
        return futureEntity;
    }

    public FutureEntity CreateEntity(Entity prototype)
    {
        if(OfWorld.PrototypeWorld is null)
            throw new InvalidOperationException("OfWorld.PrototypeWorld should not be null!");
        return CreateEntity(OfWorld.PrototypeWorld, prototype);
    }

    public void DestroyEntity(Entity entity)
    {
        _commands.Add(new ECSCommand(entity, ECSCommandTypes.Destroy));
    }

    public void Execute()
    {
        Entity Target;
        foreach(ECSCommand cmd in _commands)
        {
            switch(cmd.CommandType)
            {
            case ECSCommandTypes.Add:
                
                Target =
                    cmd.TargetsFuture ?
                    _futureEntities[cmd.FutureTarget.Index] :
                    cmd.Target;
                _components.GetSet(cmd.ComponentType!).AddToEntity(Target);

                break;

            case ECSCommandTypes.Remove:

                Target =
                    cmd.TargetsFuture ?
                    _futureEntities[cmd.FutureTarget.Index] :
                    cmd.Target;
                _components.GetSet(cmd.ComponentType!).RemoveFromEntity(Target);

                break;
                
            case ECSCommandTypes.Set:
                
                Target =
                    cmd.TargetsFuture ?
                    _futureEntities[cmd.FutureTarget.Index] :
                    cmd.Target;
                _components.GetSet(cmd.ComponentType!).SetComponent(Target, cmd.ComponentSetTo!);

                break;
                
            case ECSCommandTypes.Create:

                if(cmd.Prototype.IsValid)
                    _futureEntities[cmd.FutureTarget.Index] = OfWorld.Entities.Create(cmd.PrototypeWorld!, cmd.Prototype);
                else
                    _futureEntities[cmd.FutureTarget.Index] = OfWorld.Entities.Create();

                break;

            case ECSCommandTypes.Destroy:

                Target =
                    cmd.TargetsFuture ?
                    _futureEntities[cmd.FutureTarget.Index] :
                    cmd.Target;
                OfWorld.Entities.Destroy(Target);

                break;
            }
        }

        Clear();
    }

    public void Clear()
    {
        _commands.Clear();
        _futureEntities.Clear();
        _futureEntities.Add(default);
    }
}
