using Godot;
using EECS.Core;
using EECS.Core.Components;
using System.ComponentModel;

namespace EECS.Godot;

public class GodotWorld : World
{
    public readonly NodeManager Nodes;
	public GodotWorld(string name, int maxEntityID) : base(name, maxEntityID)
    {
        Nodes = new NodeManager();
    }

}

public class NodeManager
{
    private Dictionary<Entity, Node> _entityToNode;
    private Dictionary<Node, Entity> _nodeToEntity;
    public NodeManager()
    {
        _entityToNode = new();
        _nodeToEntity = new();
    }

    public Entity GetEntity(Node node)
    {
        
    }

    public void BindNode(Node node, Entity entity)
    {
        _entityToNode.Add(entity, node);
    }
}
