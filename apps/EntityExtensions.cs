using System;
using System.Linq;
using JoySoftware.HomeAssistant.NetDaemon.Common;

public static class EntityExtensions
{
    public static bool AllStatesAre(this NetDaemonApp app, Func<IEntityProperties, bool> entityFilter, string desiredState)
    {
       return app.State.Where(entityFilter).All(e => e.State == desiredState);
    }

    public static bool AllStatesAre(this NetDaemonApp app, Func<IEntityProperties, bool> entityFilter, params string[] desiredStates)
    {
        return app.State.Where(entityFilter).All(e =>  desiredStates.ToList().Contains(e.State));
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
