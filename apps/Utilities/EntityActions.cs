using System.Linq;
using System.Threading.Tasks;
using JoySoftware.HomeAssistant.NetDaemon.Common;

public static class EntityActions
{
    public static async Task TurnEverythingOff(this NetDaemonApp app, string? roomName= null, params string[] excludeEntities)
    {
        bool EntitiesToTurnOff(IEntityProperties e) =>
            (e.EntityId.StartsWith("light.") ||
             e.EntityId.StartsWith("fan.") ||
             e.EntityId.StartsWith("climate.")) &&
            !excludeEntities.ToList().Contains(e.EntityId);
        
        if (roomName == null)
            await app.Entities(e => e.EntityId.StartsWith("light.")).TurnOff().ExecuteAsync();
        else
        {
            bool Area(IEntityProperties e) => e.Attribute!.area != null && ((string)e.Attribute!.area).Split(",").Contains(roomName!.ToLower());

            await app.Entities(e => Area(e) && EntitiesToTurnOff(e)).TurnOff().ExecuteAsync();
        }

        // TODO turn off switches not marked as always on
        // dont turn off bedroom climate if occupied
        // alert if any windows doors open
    }
}