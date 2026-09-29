using Godot;
using EECS.Core;
using EECS.Core.Components;
using System.ComponentModel;
using System.Net.Http.Headers;

namespace EECS.Godot;

/// <summary>
/// 
/// </summary>
[Component]
public struct NodeSync
{
    PackedScene TargetScene;
    NodePath TargetParent;
}

public class NodeSyncManager
{
    public GodotWorld OfWorld;
    public NodeSyncManager(GodotWorld world)
    {
        OfWorld = world;
    }

    public void Sync()
    {
        foreach(Entity entity in
                    new Query(OfWorld)
                    .With<NodeSync>()
                    .Optional<NodeBind>()
                    .ExecuteSnapshot())
        {
            
        }
    }
}
