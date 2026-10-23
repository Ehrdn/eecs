using EECS.Core;
using EECS.Core.Components;
using EECS.Core.Events;

namespace EECS.Test_Core.Events;

[TestFixture]
public class EventsTests
{
    private enum EventOrder
    {
        Before,
        Main,
        After
    }

    private struct EventParameters
    {
        public int Value;
        public List<string> Calls;
    }

    private struct OrderedParameters
    {
        public List<string> Calls;
    }

    private World _world = null!;

    [SetUp]
    public void Setup()
    {
        _world = new World();
    }
    
    [Test]
    public void EntityCondition_WithWithoutAndPredicate_ShouldMatchExpectedEntities()
    {
        Entity withPosition = _world.Entities.Create();
        Entity withPositionAndVelocity = _world.Entities.Create();
        Entity withoutPosition = _world.Entities.Create();

        ComponentPool<Position> positions = _world.Components.GetSet<Position>();
        ComponentPool<Velocity> velocities = _world.Components.GetSet<Velocity>();
        positions.AddToEntity(withPosition);
        positions.AddToEntity(withPositionAndVelocity);
        velocities.AddToEntity(withPositionAndVelocity);

        EntityCondition condition = new(
            _world,
            entity => entity == withPosition);
        condition.With<Position>().Without<Velocity>();

        Assert.Multiple(() =>
        {
            Assert.That(condition.Satisfied(withPosition), Is.True);
            Assert.That(condition.Satisfied(withPositionAndVelocity), Is.False);
            Assert.That(condition.Satisfied(withoutPosition), Is.False);
        });
    }

    [Test]
    public void EntityEvent_EventAndHandlerFilters_ShouldOnlyInvokeMatchingHandlers()
    {
        Entity positionOnly = _world.Entities.Create();
        Entity positionAndVelocity = _world.Entities.Create();
        Entity velocityOnly = _world.Entities.Create();

        ComponentPool<Position> positions = _world.Components.GetSet<Position>();
        ComponentPool<Velocity> velocities = _world.Components.GetSet<Velocity>();
        positions.AddToEntity(positionOnly);
        positions.AddToEntity(positionAndVelocity);
        velocities.AddToEntity(positionAndVelocity);
        velocities.AddToEntity(velocityOnly);

        EntityEvent<EventOrder, EventParameters> entityEvent = new(
            new EntityCondition(_world).With<Position>());
        entityEvent.RegisterHandler(AddOne, EventOrder.Main);
        entityEvent.RegisterHandler(AddTen, EventOrder.Main,
            filter: new EntityCondition(_world).Without<Velocity>());

        EventParameters parameters = new() { Calls = [] };
        entityEvent.Fire(positionOnly, ref parameters);
        Assert.That(parameters.Value, Is.EqualTo(11));

        parameters = new() { Calls = [] };
        entityEvent.Fire(positionAndVelocity, ref parameters);
        Assert.That(parameters.Value, Is.EqualTo(1));

        parameters = new() { Calls = [] };
        entityEvent.Fire(velocityOnly, ref parameters);
        Assert.That(parameters.Value, Is.EqualTo(0));
    }

    [Test]
    public void EntityEvent_ShouldRunHandlersByOrderAndSequenceAndPassRefParameters()
    {
        Entity entity = _world.Entities.Create();
        EntityEvent<EventOrder, EventParameters> entityEvent = new();
        entityEvent.RegisterHandler(AddOneAndRecord, EventOrder.Main, sequence: 10);
        entityEvent.RegisterHandler(AddTenAndRecord, EventOrder.Before);
        entityEvent.RegisterHandler(AddHundredAndRecord, EventOrder.Main, sequence: 0);

        EventParameters parameters = new() { Calls = [] };
        entityEvent.Fire(entity, ref parameters);

        Assert.Multiple(() =>
        {
            Assert.That(parameters.Value, Is.EqualTo(111));
            Assert.That(parameters.Calls, Is.EqualTo(["ten", "hundred", "one"]));
        });
    }

    [Test]
    public void OrderedEvent_ShouldRunHandlersByOrderAndSequence()
    {
        OrderedEvent<EventOrder, OrderedParameters> orderedEvent = new();
        orderedEvent.RegisterHandler(RecordFirst, EventOrder.After);
        orderedEvent.RegisterHandler(RecordSecond, EventOrder.Before, 5);
        orderedEvent.RegisterHandler(RecordThird, EventOrder.Before, 0);

        OrderedParameters parameters = new() { Calls = [] };
        orderedEvent.Fire(ref parameters);

        Assert.That(parameters.Calls, Is.EqualTo(["third", "second", "first"]));
    }

    private static ref EventParameters AddOne(Entity entity, ref EventParameters parameters)
    {
        parameters.Value += 1;
        return ref parameters;
    }

    private static ref EventParameters AddTen(Entity entity, ref EventParameters parameters)
    {
        parameters.Value += 10;
        return ref parameters;
    }

    private static ref EventParameters AddOneAndRecord(Entity entity, ref EventParameters parameters)
    {
        parameters.Value += 1;
        parameters.Calls.Add("one");
        return ref parameters;
    }

    private static ref EventParameters AddTenAndRecord(Entity entity, ref EventParameters parameters)
    {
        parameters.Value += 10;
        parameters.Calls.Add("ten");
        return ref parameters;
    }

    private static ref EventParameters AddHundredAndRecord(Entity entity, ref EventParameters parameters)
    {
        parameters.Value += 100;
        parameters.Calls.Add("hundred");
        return ref parameters;
    }

    private static ref OrderedParameters RecordFirst(ref OrderedParameters parameters)
    {
        parameters.Calls.Add("first");
        return ref parameters;
    }

    private static ref OrderedParameters RecordSecond(ref OrderedParameters parameters)
    {
        parameters.Calls.Add("second");
        return ref parameters;
    }

    private static ref OrderedParameters RecordThird(ref OrderedParameters parameters)
    {
        parameters.Calls.Add("third");
        return ref parameters;
    }
}