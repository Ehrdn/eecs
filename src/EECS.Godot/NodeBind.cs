using Godot;
using EECS.Core;
using EECS.Core.Components;
using System.ComponentModel;

namespace EECS.Godot;

/// <summary>
/// Entity with this Component has the ability to bind with a Godot node.
/// This component exists purely for filtering nodes with the ability to bind with entities in Query.
/// Access whether or not it is binded with a node through `NodeBindsManager` in `GodotWorld`.
/// </summary>
[Component("Godot.NodeBind")]
public struct NodeBind
{
    internal Node? BindedNode;
    public NodeBind(Node? bindedNode)
    {
        this.BindedNode = bindedNode;
    }
}

public class NodeBindsManager
{
    public readonly GodotWorld OfWorld;
    private Dictionary<ulong, Entity> _nodeBindCache;
    private readonly ComponentPool<NodeBind> _nodeBindPool;

    public NodeBindsManager(GodotWorld world)
    {
        _nodeBindCache = new();
        _nodeBindPool = world.Components.GetSet<NodeBind>();
        OfWorld = world;
    }

    /// <summary>
    /// Update _nodeBindCache to remove invalid node binding cache.
    /// </summary>
    /// <param name="node"></param>
    private void UpdateNodeCache(Node node)
    {
        ulong nodeID = node.GetInstanceId();
        if(!_nodeBindCache.ContainsKey(nodeID))
            return;
        Entity entity = _nodeBindCache[nodeID];
        if(!OfWorld.Entities.IsAlive(entity)
            || !OfWorld.Components.Has<NodeBind>(entity)
            || _nodeBindPool[entity].BindedNode != node)
            _nodeBindCache.Remove(nodeID);
    }

    /// <summary>
    /// Unbind the specified entity if it is binded with an invalid node.
    /// </summary>
    /// <param name="entity"></param>
    private void UnbindInvalidNode(Entity entity)
    {
        if(!OfWorld.Entities.IsAlive(entity)
            || !_nodeBindPool.EntityHasComponent(entity))
            return;
        NodeBind nodeBind = _nodeBindPool[entity];
        if(!GodotObject.IsInstanceValid(nodeBind.BindedNode))
            _nodeBindPool[entity].BindedNode = null;
    }

    /// <summary>
    /// Returns the entity that the node is binded with.
    /// </summary>
    /// <param name="node"></param>
    /// <returns></returns>
    public Entity GetBinded(Node node)
    {
        UpdateNodeCache(node);
        if(!_nodeBindCache.ContainsKey(node.GetInstanceId()))
            throw new InvalidOperationException($"Node {node} is not binded with any entity.");
        ulong nodeID = node.GetInstanceId();
        return _nodeBindCache[nodeID];
    }
    /// <summary>
    /// Returns the node that the entity is binded with.
    /// </summary>
    /// <param name="entity"></param>
    /// <returns></returns>
    public Node GetBinded(Entity entity)
    {
        if(!_nodeBindPool.EntityHasComponent(entity))
            throw new InvalidOperationException($"Entity {entity} does not have NodeBind Component.");
        UnbindInvalidNode(entity);
        if(_nodeBindPool[entity].BindedNode == null)
            throw new InvalidOperationException($"Entity {entity} is not binded with any node.");
        return _nodeBindPool[entity].BindedNode!;
    }

    /// <summary>
    /// Bind an Entity without NodeBind Component with a node.
    /// Throws an exception if the Entity does not have NodeBind Component. Add the Component through ECS first before binding.
    /// Throws an exception if the node/entity is already binded with another entity/node.
    /// </summary>
    /// <param name="entity"></param>
    /// <param name="node"></param>
    /// <exception cref="InvalidOperationException"></exception>
    public void Bind(Entity entity, Node node)
    {
        if(!_nodeBindPool.EntityHasComponent(entity))
            throw new InvalidOperationException($"Entity {entity} does not have NodeBind Component.");
        
        UnbindInvalidNode(entity);
        if(_nodeBindPool[entity].BindedNode != null)
            throw new InvalidOperationException($"Entity {entity} is already binded with a node.");
        
        UpdateNodeCache(node);
        ulong nodeID = node.GetInstanceId();
        if(_nodeBindCache.ContainsKey(nodeID))
            throw new InvalidOperationException($"Node {node} is already binded with an entity.");
        
        _nodeBindPool[entity].BindedNode = node;
        _nodeBindCache[nodeID] = entity;
    }

    public bool IsBinded(Entity entity)
    {
        UnbindInvalidNode(entity);
        if(_nodeBindPool.EntityHasComponent(entity))
            return _nodeBindPool[entity].BindedNode != null;
        return false;
    }
    public bool IsBinded(Node node)
    {
        UpdateNodeCache(node);
        return _nodeBindCache.ContainsKey(node.GetInstanceId());
    }

    /// <summary>
    /// Unbind a binded entity with its binded node.
    /// Does nothing if the entity is not binded with any node.
    /// </summary>
    /// <param name="entity"></param>
    public void Unbind(Entity entity)
    {
        if(!IsBinded(entity))
            return;
        Node node = GetBinded(entity);
        _nodeBindPool[entity].BindedNode = null;
        _nodeBindCache.Remove(node.GetInstanceId());
    }
    /// <summary>
    /// Unbind a binded node with its binded entity.
    /// Does nothing if the node is not binded with any entity.
    /// </summary>
    /// <param name="node"></param>
    public void Unbind(Node node)
    {
        if(!IsBinded(node))
            return;
        Entity entity = GetBinded(node)!;
        _nodeBindPool[entity].BindedNode = null;
        _nodeBindCache.Remove(node.GetInstanceId());
    }

    /// <summary>
    /// Unbind all Entities and Nodes.
    /// </summary>
    public void Reset()
    {
        foreach(Entity entity
                in new Query(OfWorld)
                .With<NodeBind>()
                .ExecuteSnapshot())
            _nodeBindPool[entity].BindedNode = null;
        _nodeBindCache.Clear();
    }
}

