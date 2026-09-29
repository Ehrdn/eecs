using Godot;
using EECS.Core;
using EECS.Core.Components;
using System.ComponentModel;
using System.Net.Http.Headers;

namespace EECS.Godot;

public class GodotWorld : World
{
    public NodeBindsManager NodeBinds;
    public NodeSyncManager NodeSyncs;
    public GodotWorld() : this(World.DefaultMaxEntityID) {}
    public GodotWorld(int maxEntityID) : base(maxEntityID)
    {
        NodeBinds = new NodeBindsManager(this);
        NodeSyncs = new NodeSyncManager(this);
    }
    public override void Reset()
    {
        base.Reset();
        NodeBinds.Reset();
    }
}
