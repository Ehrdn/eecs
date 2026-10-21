using EECS.Core;
using EECS.Core.Components;

namespace EECS.Components;

public struct ComponentCommand
{
    public enum CommandType
    {
        Add,
        Remove,
        Set
    }
    public Type ComponentType;
    public Entity entity;
}

public class ComponentsCommandBuffer
{
    ComponentsManager OfManager;

    public ComponentsCommandBuffer(ComponentsManager ofManager)
    {
        OfManager = ofManager;
    }

    
}
