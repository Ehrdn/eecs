using System.Diagnostics.CodeAnalysis;
using EECS.Core.Components;
using EECS.Core;


namespace EECS.Core
{
    public partial class World
    {
        /// <summary>
        /// Where the prototypes of this world are stored.
        /// </summary>
        public World? PrototypeWorld { get; set; }
    }
    public partial class EntitiesManager
    {
        public Entity CreateEntity(World prototypeWorld, Entity prototypeID)
        {
            Entity entity = CreateEntity();
            if(!prototypeWorld.Entities.IsAlive(prototypeID))
                throw new InvalidOperationException("Prototype entity is not alive.");
            foreach(int cid in prototypeWorld.Entities.GetEntityComponentIDs(prototypeID)!)
            {
                IComponentPool entityPool = OfWorld.Components.GetSet(cid);
                IComponentPool prototypePool = prototypeWorld.Components.GetSet(cid);
                entityPool.AddToEntity(entity);
                entityPool.SetComponent(entity, prototypePool.GetComponent(prototypeID));
            }
            return entity;
        }
        public Entity CreateEntity(Entity prototypeID)
        {
            if(OfWorld.PrototypeWorld is null)
                throw new InvalidOperationException("OfWorld.PrototypeWorld is null.");
            return CreateEntity(OfWorld.PrototypeWorld, prototypeID);
        }
    }
}
