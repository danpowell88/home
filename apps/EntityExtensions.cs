using System.Collections.Generic;
using System.Linq;
using JoySoftware.HomeAssistant.NetDaemon.Common;

public static class EntityExtensions
{
    public static bool AllStatesAre(this NetDaemonApp app, IEnumerable<string> entities, string desiredState)
    {
        return entities.ToList()
            .All(e => (app.GetState(e) != null ? (string?) app.GetState(e)?.State : null) == desiredState);
    }

    public static bool AllStatesAre(this NetDaemonApp app, IEnumerable<string> entities, params string[] desiredStates)
    {
        return entities.ToList()
            .All(e =>  desiredStates.Contains(app.GetState(e) != null ? (string?)app.GetState(e)!.State! : string.Empty));
    }

    public static bool AnyStatesAre(this NetDaemonApp app, IEnumerable<string> entities, string desiredState)
    {
        return entities
            .ToList()
            .Any(e => (app.GetState(e) != null ? (string?) app.GetState(e)?.State : null) == desiredState);
    }
}
