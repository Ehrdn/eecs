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
    public Position(int x, int y, int z)
    {
        X = x;
        Y = y;
        Z = z;
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
    public Velocity(int x, int y, int z)
    {
        X = x;
        Y = y;
        Z = z;
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
    public AnimationProgress(int frame)
    {
        Frame = frame;
    }
}

[TestFixture]
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
    public void ComponentPool_AddComponentToEntity_EntityShouldHaveSameComponent()
    {
        var posPool = _manager.GetSet<Position>();
        var velPool = _manager.GetSet<Velocity>();
        var a = _world.Entities.CreateEntity();
        var b = _world.Entities.CreateEntity();

        posPool.AddToEntity(a, new Position(7, 14, 21));
        velPool.AddToEntity(a, new Velocity(3, 5, 7));

        posPool.AddToEntity(b, new Position(123, 456, 789));
        velPool.AddToEntity(b, new Velocity(1, 2, 14));

        Assert.Multiple(() => {
            Assert.That(posPool[a], Is.EqualTo(new Position(7, 14, 21)));
            Assert.That(velPool[a], Is.EqualTo(new Velocity(3, 5, 7)));
            
            Assert.That(posPool[b], Is.EqualTo(new Position(123, 456, 789)));
            Assert.That(velPool[b], Is.EqualTo(new Velocity(1, 2, 14)));
        });
    }
    
    [Test]
    public void ComponentPool_ChangeComponentOfEntity_ComponentPoolShouldReflectChange()
    {
        var posPool = _manager.GetSet<Position>();
        var velPool = _manager.GetSet<Velocity>();
        var a = _world.Entities.CreateEntity();
        var b = _world.Entities.CreateEntity();

        posPool.AddToEntity(a, new Position(7, 14, 21));
        velPool.AddToEntity(a, new Velocity(3, 5, 7));

        posPool.AddToEntity(b, new Position(123, 456, 789));
        velPool.AddToEntity(b, new Velocity(1, 2, 14));

        posPool[a].X = 1;
        velPool.GetComponentRef(a).Y = 2;

        velPool.SetComponent(b, new Velocity(9, 8, 7));

        Assert.Multiple(() => {
            Assert.That(posPool[a], Is.EqualTo(new Position(1, 14, 21)));
            Assert.That(velPool[a], Is.EqualTo(new Velocity(3, 2, 7)));
            
            Assert.That(posPool[b], Is.EqualTo(new Position(123, 456, 789)));
            Assert.That(velPool[b], Is.EqualTo(new Velocity(9, 8, 7)));
        });
    }

    [Test]
    public void ComponentPool_AttemptAccessDeadEntity_ShouldThrowException()
    {
        var a = _world.Entities.CreateEntity();
        var b = _world.Entities.CreateEntity();
        var posPool = _manager.GetSet<Position>();
        var velPool = _manager.GetSet<Velocity>();
        var animPool = _manager.GetSet<AnimationProgress>();

        posPool.AddToEntity(a);
        velPool.AddToEntity(a);

        posPool.AddToEntity(b);
        animPool.AddToEntity(b);

        _world.Entities.DestroyEntity(b);

        Assert.That(() => posPool[b], Throws.ArgumentException);
    }
    
    [Test]
    public void ComponentPool_AttemptAccessEntityWithoutThisComponent_ShouldThrowException()
    {
        var a = _world.Entities.CreateEntity();
        var b = _world.Entities.CreateEntity();
        var posPool = _manager.GetSet<Position>();
        var velPool = _manager.GetSet<Velocity>();
        var animPool = _manager.GetSet<AnimationProgress>();

        posPool.AddToEntity(a);
        velPool.AddToEntity(a);
        animPool.AddToEntity(a);
        animPool.RemoveFromEntity(a);

        posPool.AddToEntity(b);
        animPool.AddToEntity(b);

        Assert.Multiple(() =>{
            Assert.That(() => animPool[a], Throws.InvalidOperationException);
            Assert.That(() => velPool[b], Throws.InvalidOperationException);
        });
    }

    [Test]
    public void ComponentPool_RemoveEntity_ShouldNotHaveEntityInDense()
    {
        var a = _world.Entities.CreateEntity();
        var b = _world.Entities.CreateEntity();
        var c = _world.Entities.CreateEntity();
        var d = _world.Entities.CreateEntity();
        var posPool = _manager.GetSet<Position>();
        var velPool = _manager.GetSet<Velocity>();
        var animPool = _manager.GetSet<AnimationProgress>();

        posPool.AddToEntity(a);
        velPool.AddToEntity(a);

        posPool.AddToEntity(b);
        animPool.AddToEntity(b);
        
        posPool.AddToEntity(c);
        velPool.AddToEntity(c);
        animPool.AddToEntity(c);

        velPool.AddToEntity(d);
        animPool.AddToEntity(d);

        int posPoolBeforeCount = posPool.Count;
        int velPoolBeforeCount = velPool.Count;
        int animPoolBeforeCount = animPool.Count;

        posPool.RemoveFromEntity(a);
        velPool.RemoveFromEntity(d);
        animPool.RemoveFromEntity(b);

        Assert.Multiple(() =>{
           Assert.That(posPool.Count, Is.LessThan(posPoolBeforeCount)); 
           Assert.That(velPool.Count, Is.LessThan(velPoolBeforeCount));
           Assert.That(animPool.Count, Is.LessThan(animPoolBeforeCount)); 
        });
    }
}
