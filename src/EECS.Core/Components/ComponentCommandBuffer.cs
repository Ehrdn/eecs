using EECS.Core;
using EECS.Core.Components;

namespace EECS.Components;

public readonly struct ComponentCommand
{
    public enum CommandTypes
    {
        Add,
        Remove,
        Set
    }
    public readonly Entity Target;
    public readonly Type ComponentType;
    public readonly CommandTypes CommandType;
    
    public readonly object? ComponentToSet;
    
    public ComponentCommand(Entity entity, Type componentType, CommandTypes commandType)
    {
        Target = entity;
        ComponentType = componentType;
        CommandType = commandType;

    }
}

public class ComponentsCommandBuffer
{
    public readonly World OfWorld;
    private ComponentsManager _components;
    private List<ComponentCommand> _commands;

    public ComponentsCommandBuffer(World ofWorld)
    {
        OfWorld = ofWorld;
        _components = OfWorld.Components;
        _commands = new();
    }

    public void Add<T>(Entity entity)
    {
        
    }

    public void Execute()
    {
        
    }

    public void Clear()
    {
        
    }
}
