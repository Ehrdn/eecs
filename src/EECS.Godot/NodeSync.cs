using Godot;
using EECS.Core;
using EECS.Core.Components;
using System.ComponentModel;
using System;

namespace EECS.Godot;

/// <summary>
/// Requires NodeBind Component.
/// </summary>
[Component]
public struct NodeSync
{
    public PackedScene TargetScene;
    public NodePath TargetParent;
    // public delegate void SyncNodeStateDelegate(Entity entity, Node node);
    public Action<Entity, Node>? SyncNodeStateCallback;
    
    public NodeSync(
            PackedScene targetScene,
            NodePath targetParent,
            Action<Entity, Node>? syncNodeStateCallback = null
        )
    {
        TargetScene = targetScene;
        TargetParent = targetParent;
        SyncNodeStateCallback = syncNodeStateCallback;
    }
}

/// <summary>
/// NodeSyncManager with IsLayered = true will only order the synchronization of entities with NodeSyncLayered component.
/// The layer is computed based on the entity's nodeSync.rootNode or nodeSyncManager.DefaultRootNode and the node's position in the hierarchy, so make sure there's no common ancestor between nodes of different entities.
/// The order of entites with NodeSync but without NodeSyncLayered component is not guaranteed.
/// Do not Add / Remove this component manually, do it through NodeSyncManager.SetEntityLayeredState instead.
/// </summary>
[Component]
internal struct NodeSyncLayered
{
    /// <summary>
    /// The layer to which the node belongs.
    /// 1 for direct children of the root node.
    /// </summary>
    internal byte Layer { get; set; }
    public NodeSyncLayered(byte layer)
    {
        Layer = layer;
    }
}

#region NodeSyncLayerComponents
[Component] public struct NodeSyncLayer1 { }
[Component] public struct NodeSyncLayer2 { }
[Component] public struct NodeSyncLayer3 { }
[Component] public struct NodeSyncLayer4 { }
[Component] public struct NodeSyncLayer5 { }
[Component] public struct NodeSyncLayer6 { }
[Component] public struct NodeSyncLayer7 { }
[Component] public struct NodeSyncLayer8 { }
[Component] public struct NodeSyncLayer9 { }
[Component] public struct NodeSyncLayer10 { }
[Component] public struct NodeSyncLayer11 { }
[Component] public struct NodeSyncLayer12 { }
[Component] public struct NodeSyncLayer13 { }
[Component] public struct NodeSyncLayer14 { }
[Component] public struct NodeSyncLayer15 { }
[Component] public struct NodeSyncLayer16 { }
#endregion

public class NodeSyncManager
{
    public GodotWorld OfWorld;
    /// <summary>
    /// If true, the NodeSyncManager will use the Layer property of NodeSync Component to determine the order of synchronization, but introduces a considerable performance overhead.
    /// You may want this behavior if the nodes that corresponds to the entities are in a hierarchy and you want to sync the parent nodes before the child nodes.
    /// </summary>
    public readonly bool IsLayered;
    public Node? RootNode;

    private readonly ComponentPool<NodeSync> _nodeSyncPool;
    private readonly ComponentPool<NodeBind> _nodeBindPool;
    private static readonly Type[] _nodeSyncLayerTypes = new Type[]
    {
        typeof(NodeSyncLayer1),
        typeof(NodeSyncLayer2),
        typeof(NodeSyncLayer3),
        typeof(NodeSyncLayer4),
        typeof(NodeSyncLayer5),
        typeof(NodeSyncLayer6),
        typeof(NodeSyncLayer7),
        typeof(NodeSyncLayer8),
        typeof(NodeSyncLayer9),
        typeof(NodeSyncLayer10),
        typeof(NodeSyncLayer11),
        typeof(NodeSyncLayer12),
        typeof(NodeSyncLayer13),
        typeof(NodeSyncLayer14),
        typeof(NodeSyncLayer15),
        typeof(NodeSyncLayer16)
    };

    // static NodeSyncManager()
    // {
    //     foreach(Type type in _nodeSyncLayerTypes)
    //         ComponentsManager
    //         .RegisterComponentAddedDelegate(type, ())
    // }

    public NodeSyncManager(GodotWorld world, Node? rootNode, bool isLayered = false)
    {
        OfWorld = world;
        IsLayered = isLayered;
        RootNode = rootNode;
        _nodeSyncPool = world.Components.GetSet<NodeSync>();
        _nodeBindPool = world.Components.GetSet<NodeBind>();
    }

    public void SetEntityLayeredState(Entity entity, bool isLayered)
    {
        if(!IsLayered)
            throw new InvalidOperationException("NodeSyncManager is not layered. Cannot set entity layered state.");
        if(!OfWorld.Entities.IsAlive(entity))
            throw new ArgumentException($"Entity {entity} is not alive.");
        if(!OfWorld.Components.Has<NodeSync>(entity))
            throw new ArgumentException($"Entity {entity} does not have NodeSync component.");
        if(isLayered)
            OfWorld.Components.Add<NodeSyncLayered>(entity);
        else
            OfWorld.Components.Remove<NodeSyncLayered>(entity);
    }

    public bool IsEntityLayered(Entity entity)
    {
        if(!IsLayered)
            throw new InvalidOperationException("NodeSyncManager is not layered. Cannot check entity layered state.");
        if(!OfWorld.Entities.IsAlive(entity))
            throw new ArgumentException($"Entity {entity} is not alive.");
        if(!OfWorld.Components.Has<NodeSync>(entity))
            throw new ArgumentException($"Entity {entity} does not have NodeSync component.");
        return OfWorld.Components.Has<NodeSyncLayered>(entity);
    }

    private void UpdateNodeSyncLayer(Entity entity)
    {
        if(!OfWorld.Components.Has<NodeSyncLayered>(entity))
            return;
        ref NodeSync nodeSync = ref _nodeSyncPool[entity];
        
        ref NodeSyncLayered nodeSyncLayered = ref OfWorld.Components.Get<NodeSyncLayered>(entity);
        
        byte layer = (byte)nodeSync.TargetParent.GetNameCount();

        OfWorld.Components.GetSet(_nodeSyncLayerTypes[nodeSyncLayered.Layer - 1]).RemoveFromEntity(entity);

        nodeSyncLayered.Layer = layer;

        OfWorld.Components.GetSet(_nodeSyncLayerTypes[layer - 1]).AddToEntity(entity);
    }

    public void SyncAll(Query? withQuery = null)
    {
        if(withQuery == null)
            withQuery = new Query(OfWorld);
        if(withQuery.OfWorld != OfWorld)
            throw new InvalidOperationException("withQuery does not have the same OfWorld field as the NodeSync instance");
        if(IsLayered)
        {
            foreach(Entity entity in
                withQuery
                .With<NodeSyncLayered>()
                .ExecuteSnapshot())
                    UpdateNodeSyncLayer(entity);
            
            for(int i = 0; i < _nodeSyncLayerTypes.Length; i++)
            {
                Type layerType = _nodeSyncLayerTypes[i];
                foreach(Entity entity in
                    withQuery
                    .With(layerType)
                    .ExecuteSnapshot())
                    Sync(entity);
            }
            foreach(Entity entity in
                withQuery
                .With<NodeSync>()
                .Without<NodeSyncLayered>()
                .ExecuteSnapshot())
                    Sync(entity);
        }
        else
        foreach(Entity entity in
                    withQuery
                    .With<NodeSync>()
                    .ExecuteSnapshot())
            Sync(entity);
    }

    public Node Sync(Entity entity)
    {
        ref NodeSync nodeSync = ref _nodeSyncPool[entity];

        Node node;
        if(!OfWorld.NodeBinds.IsBinded(entity))
        {
            node = nodeSync.TargetScene.Instantiate<Node>();
            Node parent = RootNode!.GetNode(nodeSync.TargetParent);
            parent.AddChild(node);
            OfWorld.NodeBinds.Bind(entity, node);
        }
        else
        {
            node = OfWorld.NodeBinds.GetBinded(entity)!;
            if(node.GetParent() != RootNode!.GetNode(nodeSync.TargetParent))
            {
                Node parent = RootNode.GetNode(nodeSync.TargetParent);
                node.GetParent().RemoveChild(node);
                parent.AddChild(node);
            }
        }   
        _nodeSyncPool[entity].SyncNodeStateCallback?.Invoke(entity, node);

        return node;
    }

    /// <summary>
    /// Reset all entities that have NodeSync Component with withQuery's NodeSycn Layered State.
    /// Will Reset all entities with NodeSyncComponent if withQuery is not given.
    /// </summary>
    /// <param name="WithQuery"></param>
    /// <exception cref="InvalidOperationException"></exception>
    public void ResetEntitiesLayeredStates(Query? withQuery = null)
    {
        if(withQuery == null)
            withQuery = new Query(OfWorld);
        if(withQuery.OfWorld != OfWorld)
            throw new InvalidOperationException("withQuery does not have the same OfWorld field as the NodeSync instance");
        
        if(!IsLayered)
            throw new InvalidOperationException("NodeSyncManager is not layered. Cannot reset entity layered states.");
        foreach(Entity entity in
                    withQuery
                    .With<NodeSyncLayered>()
                    .ExecuteSnapshot())
            OfWorld.Components.Remove<NodeSyncLayered>(entity);
    }
}
