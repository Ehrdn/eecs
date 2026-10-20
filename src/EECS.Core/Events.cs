
namespace EECS.Core;

public class Event
{
    public readonly int OrderCount;
    public readonly Type OrderType;
    /// <summary>
    /// All delegate in this should have the same param list.
    /// </summary>
    public Delegate[] Handlers;


    public Event(Type orderType)
    {
        if(!orderType.IsSubclassOf(typeof(Enum)))
            throw new InvalidOperationException($"The passed orderType parameter {orderType} is not an Enum.");
        OrderType = orderType;
        OrderCount = Enum.GetNames(OrderType).Length;
        Handlers = new Delegate[OrderCount];
    }

    public void Fire()
    {
        
    }
}

