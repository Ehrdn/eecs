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
