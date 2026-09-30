using EECS.Core;
using EECS.Core.Components;
using System.Diagnostics;

namespace EECS.Test_Core;
[SetUpFixture]
public class SetupTrace
{
    [OneTimeSetUp]
    public void StartTest()
    {
        Trace.Listeners.Add(new ConsoleTraceListener());
    }

    [OneTimeTearDown]
    public void EndTest()
    {
        Trace.Flush();
    }
}

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

[Component]
public struct ComponentWithImplicitName
{
    
}
