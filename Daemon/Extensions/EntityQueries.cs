using System;
using System.Linq;
using NetDaemon.Common.Fluent;
using NetDaemon.Common.Reactive;

public static class EntityQueries
{
    public static bool AllStatesAre(this NetDaemonRxApp app, Func<IEntityProperties, bool> entityFilter, string desiredState)
    {
       return AllStatesAre(app, entityFilter, new string[] {desiredState});
    }

    public static bool AllStatesAre(this NetDaemonRxApp app, Func<IEntityProperties, bool> entityFilter, params string[] desiredStates)
    {
        return app.States.Where(s => entityFilter(s) && s.State != null).All(e => desiredStates.ToList().Contains(e.State?.ToString() ?? ""));
    }

    public static bool AnyStatesAre(this NetDaemonRxApp app, Func<IEntityProperties, bool> entityFilter, string desiredState)
    {
        return app.States.Where(s => entityFilter(s) && s.State != null).Any(e => e.State == desiredState);
    }

    public static bool AnyStatesAre(this NetDaemonRxApp app, Func<IEntityProperties, bool> entityFilter, params string[] desiredStates)
    {
        return app.States.Where(s => entityFilter(s) && s.State != null).Any(e =>desiredStates.ToList().Contains(e.State?.ToString() ?? ""));
    }

    public static bool AnyStatesAre(this NetDaemonRxApp app, Func<IEntityProperties, bool> entityFilter, Func<IEntityProperties, bool> propertyFilter)
    {
        return app.States.Where(s => entityFilter(s) && s.State != null).Any(propertyFilter);
    }

    public static void ExecuteIfTrueNowAndAfter(this NetDaemonRxApp app, Func<bool> condition, TimeSpan waitDuration, Action action)
    {
        if (condition())
        {
            app.RunIn(waitDuration, () =>
            {
                if (condition())
                    action();
            });
        }
    }
}
