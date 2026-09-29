using Godot;
using EECS.Core;
using EECS.Core.Components;
using System.ComponentModel;
using System.Security.Cryptography.X509Certificates;

namespace EECS.Godot;

/// <summary>
/// 
/// </summary>
[Component("Godot.NodeSync")]
public struct NodeSync
{
    public PackedScene? TargetScene;
    public NodePath? TargetParent;
    public delegate void SyncNodeState(Entity entity, Node node);
    public SyncNodeState? syncNodeState;
    public NodeSync(PackedScene targetScene, NodePath targetParent, SyncNodeState? syncNodeState = null)
    {
        TargetScene = targetScene;
        TargetParent = targetParent;
        this.syncNodeState = syncNodeState;
    }
    public NodeSync() { }
}

public class NodeSyncManager
{
    public GodotWorld OfWorld;
    private ComponentPool<NodeSync> _nodeSyncPool;
    private ComponentPool<NodeBind> _nodeBindPool;
    public NodeSyncManager(GodotWorld world)
    {
        OfWorld = world;
        _nodeSyncPool = world.Components.GetSet<NodeSync>();
        _nodeBindPool = world.Components.GetSet<NodeBind>();
    }

    public void SyncAll(Node rootNode)
    {
        foreach(Entity entity in
                    new Query(OfWorld)
                    .With<NodeSync>()
                    .With<NodeBind>()
                    .ExecuteSnapshot())
            Sync(rootNode, entity);
    }
    public Node Sync(Node rootNode, Entity entity)
    {
        ref NodeSync nodeSync = ref _nodeSyncPool[entity];
        ref NodeBind nodeBind = ref _nodeBindPool[entity];

        Node node = null!;
        if(!OfWorld.NodeBinds.IsBinded(entity))
        {
            node = nodeSync.TargetScene!.Instantiate<Node>();
            Node parent = rootNode.GetNode(nodeSync.TargetParent);
            parent.AddChild(node);
            OfWorld.NodeBinds.Bind(entity, node);
        }
        else
            node = OfWorld.NodeBinds.GetBinded(entity);
        _nodeSyncPool[entity].syncNodeState?.Invoke(entity, node);
        return node;
    }
}
