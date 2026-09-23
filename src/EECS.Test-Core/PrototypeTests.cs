using System.Runtime;
using EECS.Core;
using EECS.Core.Components;
using NUnit.Framework.Interfaces;

namespace EECS.Test_Core.Prototypes;

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
public class PrototypeTests
{
    private World _world = null!;
    private World _prototypeWorld = null!;

    [SetUp]
    public void Setup()
    {
        _world = new World("PrototypeTestsWorld");
        _prototypeWorld = new World("PrototypeWorld");
    }

    [TearDown]
    public void TearDown()
    {
        _world.Dispose();
        _prototypeWorld.Dispose();
    }

    [Test]
    public void Prototype_EntityCreatedFromPrototype_ShouldHaveSameComponents()
    {
        Entity pro_entity = _prototypeWorld.Entities.CreateEntity();

        ComponentPool<Position> pro_pos = _prototypeWorld.Components.GetSet<Position>();
        ComponentPool<AnimationProgress> pro_anim = _prototypeWorld.Components.GetSet<AnimationProgress>();
        ComponentPool<Velocity> pro_vel = _prototypeWorld.Components.GetSet<Velocity>();

        pro_pos.AddToEntity(pro_entity, new Position(7, 14, 21));
        pro_anim.AddToEntity(pro_entity, new AnimationProgress(12));
        pro_vel.AddToEntity(pro_entity, new Velocity(3, 5, 7));

        Entity entity = _world.Entities.CreateEntity(_prototypeWorld, pro_entity);
        ComponentPool<Position> pos = _world.Components.GetSet<Position>();
        ComponentPool<AnimationProgress> anim = _world.Components.GetSet<AnimationProgress>();
        ComponentPool<Velocity> vel = _world.Components.GetSet<Velocity>();

        Assert.Multiple(() =>
        {
            Assert.That(_world.Entities.IsAlive(entity));
            Assert.That(_world.Entities.GetEntityComponentIDs(entity), Is.EquivalentTo(
                _prototypeWorld.Entities.GetEntityComponentIDs(pro_entity)!));
            Assert.That(pos[entity], Is.EqualTo(new Position(7, 14, 21)));
            Assert.That(anim[entity], Is.EqualTo(new AnimationProgress(12)));
            Assert.That(vel[entity], Is.EqualTo(new Velocity(3, 5, 7)));
        });

        pos[entity].X = 100;
        Assert.That(pro_pos[pro_entity], Is.EqualTo(new Position(7, 14, 21)));
    }

    [Test]
    public void Prototype_CreateEntityUsingConfiguredPrototypeWorld_ShouldCopyPrototype()
    {
        _world.PrototypeWorld = _prototypeWorld;
        Entity prototype = _prototypeWorld.Entities.CreateEntity();
        ComponentPool<Position> prototypePositions = _prototypeWorld.Components.GetSet<Position>();
        prototypePositions.AddToEntity(prototype, new Position(1, 2, 3));

        Entity entity = _world.Entities.CreateEntity(prototype);

        Assert.That(_world.Components.GetSet<Position>()[entity],
            Is.EqualTo(new Position(1, 2, 3)));
    }

    [Test]
    public void Prototype_CreateEntityFromDeadPrototype_ShouldThrow()
    {
        Entity prototype = _prototypeWorld.Entities.CreateEntity();
        _prototypeWorld.Entities.DestroyEntity(prototype);

        Assert.That(
            () => _world.Entities.CreateEntity(_prototypeWorld, prototype),
            Throws.InvalidOperationException.With.Message.EqualTo("Prototype entity is not alive."));
    }
}
