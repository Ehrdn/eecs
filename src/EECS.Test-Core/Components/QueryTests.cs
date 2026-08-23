using System.Runtime;
using EECS.Core;
using EECS.Core.Components;
using NUnit.Framework.Interfaces;

namespace EECS.Test_Core.Components;

[TestFixture]
public class QueryTests
{
    public World _world = null!;
    public EntitiesManager _entities = null!;
    public ComponentsManager _components = null!;

    [SetUp]
    public void Setup()
    {
        _world = new World("QueryTests");
        _entities = _world.Entities;
        _components = _world.Components;
    }

    [TearDown]
    public void TearDown()
    {
        _world.Dispose();
    }

    [Test]
    public void Query_WithComponents_ShouldIterateEntitiesCorrectly()
    {
        var a = _world.Entities.CreateEntity();
        var b = _world.Entities.CreateEntity();
        var c = _world.Entities.CreateEntity();
        var d = _world.Entities.CreateEntity();
        var posPool = _components.GetSet<Position>();
        var velPool = _components.GetSet<Velocity>();
        var animPool = _components.GetSet<AnimationProgress>();

        posPool.AddToEntity(a);
        velPool.AddToEntity(a);

        posPool.AddToEntity(b);
        animPool.AddToEntity(b);
        
        posPool.AddToEntity(c);
        velPool.AddToEntity(c);
        animPool.AddToEntity(c);

        velPool.AddToEntity(d);
        animPool.AddToEntity(d);

        List<Entity> posVels = [];
        foreach(Entity entity in new Query(_world)
                .With<Position>()
                .With<Velocity>()
                .Execute())
            posVels.Add(entity);
        
        List<Entity> posVelAnims = [];
        foreach(Entity entity in new Query(_world)
                .With<Position>()
                .With<Velocity>()
                .With<AnimationProgress>()
                .Execute())
            posVelAnims.Add(entity);

        List<Entity> posEnts = [];
        foreach(Entity entity in new Query(_world)
                .With<Position>()
                .Execute())
            posEnts.Add(entity);
        
        Assert.Multiple(() => {
           Assert.That(posVels, Is.EquivalentTo([a, c]));
           Assert.That(posVelAnims, Is.EquivalentTo([c]));
           Assert.That(posEnts, Is.EquivalentTo([a, b, c]));
        });
    }
    
    [Test]
    public void Query_WithoutComponents_ShouldIterateEntitiesCorrectly()
    {
        var a = _world.Entities.CreateEntity();
        var b = _world.Entities.CreateEntity();
        var c = _world.Entities.CreateEntity();
        var d = _world.Entities.CreateEntity();
        var posPool = _components.GetSet<Position>();
        var velPool = _components.GetSet<Velocity>();
        var animPool = _components.GetSet<AnimationProgress>();

        posPool.AddToEntity(a);
        velPool.AddToEntity(a);

        velPool.AddToEntity(b);
        
        posPool.AddToEntity(c);
        velPool.AddToEntity(c);
        animPool.AddToEntity(c);

        velPool.AddToEntity(d);
        animPool.AddToEntity(d);

        List<Entity> posVelOAnims = [];
        foreach(Entity entity in new Query(_world)
                .With<Position>()
                .With<Velocity>()
                .Without<AnimationProgress>()
                .Execute())
            posVelOAnims.Add(entity);

        List<Entity> velOPos = [];
        foreach(Entity entity in new Query(_world)
                .With<Velocity>()
                .Without<Position>()
                .Execute())
            velOPos.Add(entity);
        
        Assert.Multiple(() => {
           Assert.That(posVelOAnims, Is.EquivalentTo([a]));
           Assert.That(velOPos, Is.EquivalentTo([b, d]));
        });
    }
}
