using System.Linq;
using System.Threading.Tasks;
using JoySoftware.HomeAssistant.NetDaemon.Common;

public static class EntityActions
{
    public static async Task TurnEverythingOff(this NetDaemonApp app, params string[] excludeEntities)
    {
        await app.Entities(e =>
                (e.EntityId.StartsWith("light.") ||
                 e.EntityId.StartsWith("fan.") ||
                 e.EntityId.StartsWith("climate.")) &&
                !excludeEntities.ToList().Contains(e.EntityId))
            .TurnOff().ExecuteAsync();

        // TODO turn off switches not marked as always on
        // dont turn off bedroom climate if occupied
        // alert if any windows doors open
    }
}