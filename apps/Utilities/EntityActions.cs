using System;
using System.Linq;
using System.Threading.Tasks;
using EnumsNET;
using JoySoftware.HomeAssistant.NetDaemon.Common;

public static class EntityActions
{
    public static async Task TurnEverythingOff(this NetDaemonApp app, string? roomName= null, params string[] excludeEntities)
    {
        bool Area(IEntityProperties e) => 
            roomName == null ? 
                true :
                (e.Attribute!.area != null && ((string)e.Attribute!.area).Split(",").Contains(roomName.ToLower()));

        bool Entities(IEntityProperties e) =>
            (e.EntityId.StartsWith("light.") ||
             e.EntityId.StartsWith("fan.") ||
             e.EntityId.StartsWith("climate.")) &&
            !excludeEntities.ToList().Contains(e.EntityId);

        await app.Entities(e => Area(e) && Entities(e))
            .TurnOff().ExecuteAsync();

        // TODO turn off switches not marked as always on
        // dont turn off bedroom climate if occupied
        // alert if any windows doors open
    }
}