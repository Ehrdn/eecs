using System.Runtime;
using EECS.Core;
using EECS.Core.Components;
using NUnit.Framework.Interfaces;
using EECS.Test_Core;
using System.ComponentModel;
using System.Runtime.Versioning;
using System.Security;

namespace EECS.Test_Core.Components;

[Component("ExplicitName1")]
public struct ComponentWithExplicitName { }
[Component("ExplicitName2")]
public struct ComponentWithAnotherExplicitName { }

/// <summary>
/// Also has one with the same Name under different namespace
/// (Under EECS.Test_Core)
/// </summary>
[Component]
public struct ComponentWithImplicitName { }


[TestFixture]
public class ComponentNameTests
{
    private World _world = null!;
    [SetUp]
    public void Setup()
    {
        _world = new World();    
    }

    
    [Test]
    public void ComponentsManager_ExplicitComponentNames_ShouldStoreCorrectly()
    {
        Assert.Multiple(() => {
            Assert.That(
                ComponentsManager.ComponentNameByType[
                    typeof(EECS.Test_Core.ComponentWithImplicitName)
                ],
                Is.EqualTo("EECS.Test_Core.ComponentWithImplicitName")
            );  
        });
    }
    [Test]
    public void ComponentsManager_ImplicitComponentNames_ShouldInferCorrectly()
    {
        Assert.Multiple(() => {
        Assert.That(
                ComponentsManager.ComponentNameByType[
                    typeof(EECS.Test_Core.ComponentWithImplicitName)
                ],
                Is.EqualTo("EECS.Test_Core.ComponentWithImplicitName")
            );
            
            Assert.That(
                ComponentsManager.ComponentNameByType[
                    typeof(EECS.Test_Core.Components.ComponentWithImplicitName)
                ],
                Is.EqualTo("EECS.Test_Core.Components.ComponentWithImplicitName")
            );
        });
    }
}

[TestFixture]
public class ComponentOperationCallbackTests
{
    private World _world = null!;
    [SetUp]
    public void Setup()
    {
        _world = new World();
    }
    [TearDown]
    public void TearDown()
    {
        ComponentsManager.RemoveComponentAddedDelegate<Position>();
        ComponentsManager.RemoveComponentRemovedDelegate<Position>();
    }

    [Test]
    public void ComponentsManager_ComponentAddedCallback_ShouldBeCalled()
    {
        bool posCallbackCalled = false;
        ComponentsManager.RegisterComponentAddedDelegate<Position>((world, entity, type) => {
            posCallbackCalled = true;
            ref Position pos = ref world.Components.GetSet<Position>()[entity];
            pos.X = 7;
            pos.Y = 14;
            pos.Z = 12;
        });

        Assert.That(posCallbackCalled, Is.False);

        Entity entity = _world.Entities.CreateEntity();
        _world.Components.GetSet<Position>().AddToEntity(entity);

        Assert.Multiple(() => {
            Assert.That(posCallbackCalled, Is.True);
            Assert.That(_world.Components.GetSet<Position>()[entity], Is.EqualTo(new Position(7, 14, 12)));
        });
    }
    
    [Test]
    public void ComponentsManager_ComponentRemovedCallback_ShouldBeCalled()
    {
        Position posBeforeRemoval = default;
        ComponentsManager.RegisterComponentRemovedDelegate<Position>((world, entity, type) => {
            posBeforeRemoval = world.Components.GetSet<Position>()[entity];
        });

        Assert.That(posBeforeRemoval, Is.EqualTo(default(Position)));

        Entity entity = _world.Entities.CreateEntity();
        _world.Components.GetSet<Position>().AddToEntity(entity);
        _world.Components.GetSet<Position>()[entity] = new Position(32, 124, 124);
        _world.Components.GetSet<Position>().RemoveFromEntity(entity);

        Assert.Multiple(() => {
            Assert.That(posBeforeRemoval, Is.EqualTo(new Position(32, 124, 124)));
            Assert.That(_world.Components.GetSet<Position>().EntityHasComponent(entity), Is.False);
        });
    }

    public void ComponentsManager_ComponentAddedCallbackRemoved_ShouldNotBeCalled()
    {
        bool posCallbackCalled = false;
        ComponentsManager.RegisterComponentAddedDelegate<Position>((world, entity, type) => {
            posCallbackCalled = true;
        });
        ComponentsManager.RemoveComponentAddedDelegate<Position>();

        Assert.That(posCallbackCalled, Is.False);

        Entity entity = _world.Entities.CreateEntity();
        _world.Components.GetSet<Position>().AddToEntity(entity);

        Assert.That(posCallbackCalled, Is.False);
    }
}