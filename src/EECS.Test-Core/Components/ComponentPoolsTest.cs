using System.Runtime;
using EECS.Core;
using EECS.Core.Components;
using NUnit.Framework.Interfaces;

namespace EECS.Test_Core.Components;

[Component]
struct Position
{
    public int X, Y, Z;
    public Position()
    {
        X = Y = Z = 0;
    }
}
[Component]
struct Velocity
{
    public int X, Y, Z;
    public Velocity()
    {
        X = Y = Z = 0;
    }
}
[Component]
struct AnimationProgress
{
    public int Frame;
    public AnimationProgress()
    {
        Frame = 0;
    }
}

public class ComponentPoolTests
{
    private ComponentsManager _manager => _world.Components;
    private World _world = null!;

    [SetUp]
    public void Setup()
    {
        _world = new World("ComponentPoolTestsWorld");
    }

    [TearDown]
    public void TearDown()
    {
        _world.Dispose();
    }

    [Test]
    public void ComponentPool_AddComponentToEntity_EntityShouldHaveComponent()
    {
        var posPool = _manager.GetSet<Position>();
        var velPool = _manager.GetSet<Velocity>();
        var a = _world.Entities.CreateEntity();
        var b = _world.Entities.CreateEntity();

        

        posPool.AddToEntity(a, new Position());
    }
}
