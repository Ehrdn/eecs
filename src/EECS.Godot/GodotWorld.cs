using Godot;
using EECS.Core;
using EECS.Core.Components;
using System.ComponentModel;
using System.Net.Http.Headers;

namespace EECS.Godot;

public class GodotWorld : World
{
    public NodeBindsManager NodeBinds;
    public NodeSyncManager? NodeSync;
    public GodotWorld()
    {
        NodeBinds = new NodeBindsManager(this);
    }
    public GodotWorld(int maxEntityCount, Node? nodeSyncRootNode, bool isNodeSyncLayered = false) : base(maxEntityCount)
    {
        NodeBinds = new NodeBindsManager(this);
        NodeSync = new NodeSyncManager(this, nodeSyncRootNode, isNodeSyncLayered);
    }
    public override void Reset()
    {
        base.Reset();
        NodeBinds.Reset();
    }
}
