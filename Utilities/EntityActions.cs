using System.Linq;
using NetDaemon.Common.Fluent;
using NetDaemon.Common.Reactive;

namespace daemonapp.Utilities
{
    public static class EntityActions
    {
        public static void TurnEverythingOff(this NetDaemonRxApp app, string? roomName= null, params string[] excludeEntities)
        {
            bool EntitiesToTurnOff(IEntityProperties e) =>
                (e.EntityId.StartsWith("light.") ||
                 e.EntityId.StartsWith("fan.") ||
                 e.EntityId.StartsWith("climate.")) &&
                !excludeEntities.ToList().Contains(e.EntityId);

            if (roomName == null)
                app.Entities(e => e.EntityId.StartsWith("light.")).TurnOff();
            else
            {
                bool Area(IEntityProperties e) => e.Attribute!.area != null &&
                                                  ((string) e.Attribute!.area).Split(",").Contains(roomName!.ToLower());

                app.Entities(e => Area(e) && EntitiesToTurnOff(e)).TurnOff();
            }

            // TODO turn off switches not marked as always on
            // dont turn off bedroom climate if occupied
            // alert if any windows doors open
        }

        public static void SetVolume(this NetDaemonRxApp app, decimal volume, string entityId)
        {
            if (((decimal?)app.State(entityId)!.Attribute!.volume_level).GetValueOrDefault(0) != volume)
            {
                app.CallService("media_player", "volume_set", new
                {
                    entity_id = entityId,
                    volume_level = volume
                });
            }
        }

        //public static async Task Toggle(this IEntity entity, bool on)
        //{
        //    if (on)
        //        await entity.TurnOn().ExecuteAsync();
        //    else
        //    {
        //        await entity.TurnOff().ExecuteAsync();
        //    }
        //}
    }
}