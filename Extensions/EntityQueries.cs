using System;
using System.Linq;
using System.Threading.Tasks;
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

    public static async Task<bool> TrueNowAndAfter(this NetDaemonRxApp app, Func<bool> condition, TimeSpan waitDuration)
    {
        if (condition())
        {
            await Task.Delay(waitDuration);

            return condition();
        }

        return false;
    }
}
