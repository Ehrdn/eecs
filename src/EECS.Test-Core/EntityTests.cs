using System.Runtime;
using EECS.Core;
using EECS.Core.Components;
using NUnit.Framework.Interfaces;

namespace EECS.Test_Core.Entities;

[TestFixture]
public class EntityTests
{
    [Test]
    public void Entity_IsHashable()
    {
        Entity a = new(1, 0);
        Entity b = new(1, 1);
        Entity c = new(7, 0);

        var dict = new Dictionary<Entity, int>();
        dict[a] = 7;
        dict[b] = 14;
        dict[c] = 21;

        Assert.That(dict[a], Is.EqualTo(7));
        Assert.That(dict[b], Is.EqualTo(14));
        Assert.That(dict[c], Is.EqualTo(21));
    }
    [Test]
    public void Entity_PacksAndUnpacksCorrectly()
    {
        var entity = new Entity(123, 456);

        Assert.Multiple(() => {
            Assert.That(entity.ID, Is.EqualTo(123));
            Assert.That(entity.Generation, Is.EqualTo(456));
        });
    }
    [Test]
    public void Entity_EqualityUsesFullValue()
    {
        var a = new Entity(1, 2);
        var b = new Entity(1, 2);
        var c = new Entity(1, 3);

        Assert.Multiple(() => {
            Assert.That(a, Is.EqualTo(b));
            Assert.That(a, Is.Not.EqualTo(c));
        });
    }
}

public class EntitiesManagerTests
{
    private World _world = null!;
    private EntitiesManager _manager => _world.Entities;

    [SetUp]
    public void Setup()
    {
        _world = new World();
    }

    [TearDown]
    public void TearDown()
    {
        
    }

    [Test]
    public void Entity_IDReuse_ShouldHaveDifferentGeneration()
    {
        Entity a = _manager.CreateEntity();
        Entity b = _manager.CreateEntity();
        Entity c = _manager.CreateEntity();
        
        _manager.DestroyEntity(c);
        _manager.DestroyEntity(b);
        b = _manager.CreateEntity();
        c = _manager.CreateEntity();

        Assert.Multiple(() => {
            Assert.That(a.Generation, Is.EqualTo(0));
            Assert.That(b.Generation, Is.EqualTo(1));
            Assert.That(c.Generation, Is.EqualTo(1));
        });
        
        Assert.Multiple(() => {
            Assert.That(_manager.GetEntityGeneration(a.ID), Is.EqualTo(0));
            Assert.That(_manager.GetEntityGeneration(b.ID), Is.EqualTo(1));
            Assert.That(_manager.GetEntityGeneration(c.ID), Is.EqualTo(1));
        });
    }

    [Test]
    public void Entity_IDReuse_IDShouldBeReused()
    {
        Entity a = _manager.CreateEntity();
        Entity b = _manager.CreateEntity();
        Entity c = _manager.CreateEntity();
        
        _manager.DestroyEntity(c);
        _manager.DestroyEntity(b);
        b = _manager.CreateEntity();
        c = _manager.CreateEntity();

        Assert.Multiple(() => {
            Assert.That(a.ID, Is.LessThanOrEqualTo(2));
            Assert.That(b.ID, Is.LessThanOrEqualTo(2));
            Assert.That(c.ID, Is.LessThanOrEqualTo(2));
            Assert.That(_manager.EntityIDCount, Is.LessThanOrEqualTo(3));
        });
    }

    [Test]
    public void Entity_Destroyed_ShouldNotBeAlive()
    {
        Entity a = _manager.CreateEntity();
        Entity b = _manager.CreateEntity();
        Entity c = _manager.CreateEntity();
        
        _manager.DestroyEntity(a);
        _manager.DestroyEntity(b);
        b = _manager.CreateEntity();

        Assert.Multiple(() => {
            Assert.That(_manager.IsAlive(a), Is.False);
            Assert.That(_manager.IsAlive(b), Is.True);
            Assert.That(_manager.IsAlive(c), Is.True);
        });
    }

    [Test]
    public void Entity_Destroyed_ShouldRemoveComponents()
    {
        Entity a = _manager.CreateEntity();
        Entity b = _manager.CreateEntity();

        _world.Components.Add<Position>(a);
        _world.Components.Add<Position>(b);
        _world.Components.Add<Velocity>(a);
        _world.Components.Add<Velocity>(b);
        _world.Components.Add<AnimationProgress>(a);
        _world.Components.Add<AnimationProgress>(b);

        _manager.DestroyEntity(a);

        Assert.Multiple(() => {
            Assert.That(_world.Components.Has<Position>(a), Is.False);
            Assert.That(_world.Components.Has<Velocity>(a), Is.False);
            Assert.That(_world.Components.Has<AnimationProgress>(a), Is.False);

            Assert.That(_world.Components.Has<Position>(b), Is.True);
            Assert.That(_world.Components.Has<Velocity>(b), Is.True);
            Assert.That(_world.Components.Has<AnimationProgress>(b), Is.True);
        });
    }

    [Test]
    public void Entity_AddedComponent_ManagerShouldSyncEntityComponentIDs()
    {
        var a = _manager.CreateEntity();
        var b = _manager.CreateEntity();
        int posID = ComponentsManager.ComponentIDByType[typeof(Position)];
        int velID = ComponentsManager.ComponentIDByType[typeof(Velocity)];
        int animID = ComponentsManager.ComponentIDByType[typeof(AnimationProgress)];

        _world.Components.Add<Position>(a);
        _world.Components.Add<Position>(b);

        _world.Components.Add<Velocity>(a);

        _world.Components.Add<AnimationProgress>(b);

        Assert.Multiple(() => {
            Assert.That(
                _manager.GetEntityComponentIDs(a),
                Is.EquivalentTo([posID, velID])
            );
            Assert.That(
                _manager.GetEntityComponentIDs(b),
                Is.EquivalentTo([posID, animID])
            );
        });
    }

    [Test]
    public void Entity_RemovedComponent_ManagerShouldSyncEntityComponentIDs()
    {
        var a = _manager.CreateEntity();
        var b = _manager.CreateEntity();
        int posID = ComponentsManager.ComponentIDByType[typeof(Position)];
        int velID = ComponentsManager.ComponentIDByType[typeof(Velocity)];
        int animID = ComponentsManager.ComponentIDByType[typeof(AnimationProgress)];

        _world.Components.Add<Position>(a);
        _world.Components.Add<Position>(b);

        _world.Components.Add<Velocity>(a);
        _world.Components.Add<Velocity>(b);

        _world.Components.Add<AnimationProgress>(a);
        _world.Components.Add<AnimationProgress>(b);
        
        Assert.Multiple(() => {
            Assert.That(
                _manager.GetEntityComponentIDs(a),
                Is.EquivalentTo([posID, velID, animID])
            );
            Assert.That(
                _manager.GetEntityComponentIDs(b),
                Is.EquivalentTo([posID, velID, animID])
            );
        });

        _world.Components.Remove<AnimationProgress>(a);
        _world.Components.Remove<Velocity>(b);

        Assert.Multiple(() => {
            Assert.That(
                _manager.GetEntityComponentIDs(a),
                Is.EquivalentTo([posID, velID])
            );
            Assert.That(
                _manager.GetEntityComponentIDs(b),
                Is.EquivalentTo([posID, animID])
            );
        });
    }
}
