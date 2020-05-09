using System;
using System.Linq;
using JoySoftware.HomeAssistant.NetDaemon.Common;

public static class EntityQueries
{
    public static bool AllStatesAre(this NetDaemonApp app, Func<IEntityProperties, bool> entityFilter, string desiredState)
    {
       return AllStatesAre(app, entityFilter, new string[] {desiredState});
    }

    public static bool AllStatesAre(this NetDaemonApp app, Func<IEntityProperties, bool> entityFilter, params string[] desiredStates)
    {
        return app.State.Where(entityFilter).All(e => desiredStates.ToList().Contains(e.State?.ToString() ?? ""));
    }

    public static bool AnyStatesAre(this NetDaemonApp app, Func<IEntityProperties, bool> entityFilter, string desiredState)
    {
        return app.State.Where(entityFilter).Any(e => e.State == desiredState);
    }

    public static bool AnyStatesAre(this NetDaemonApp app, Func<IEntityProperties, bool> entityFilter, Func<IEntityProperties, bool> propertyFilter)
    {
        return app.State.Where(entityFilter).Any(propertyFilter);
    }
}
