using SEDiscordBridge.Entities.Base;
using VRage.Game;
using VRage.Game.ModAPI;
using VRage.ObjectBuilders;

namespace SEDiscordBridge.Extensions
{
    public static class ObjectBuilderExtensions
    {

        public static UniqueEntityId GetUniqueId(this MyObjectBuilder_Base self)
        {
            return new UniqueEntityId(self.GetId());
        }

        public static MyDefinitionId GetDefinitionId(this IMyInventoryItem self)
        {
            var myObjectBuilder_PhysicalObject = self.Content as MyObjectBuilder_PhysicalObject;
            if (myObjectBuilder_PhysicalObject != null)
            {
                return myObjectBuilder_PhysicalObject.GetObjectId();
            }

            return new MyDefinitionId(self.Content.TypeId, self.Content.SubtypeId);
        }

    }

}