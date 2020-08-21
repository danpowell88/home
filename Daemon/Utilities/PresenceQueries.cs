using System.Linq;
using NetDaemon.Common.Reactive;

public static class PresenceQueries
{
    public static bool IsAnyoneHome(this NetDaemonRxApp app)
    {
        return app.State("group.family")!.State == "home";
    }

    public static bool IsAnyoneSleeping(this NetDaemonRxApp app)
    {
        // todo: bayesian sensor to detect either sleeping
        return app.State("binary_sensor.bed_occupancy")!.State == "on";
    }

    public static bool IsAnyoneInBed(this NetDaemonRxApp app)
    {
        return app.State("binary_sensor.bed_occupancy")!.State == "on";
    }

    public static bool IsEveryoneInBed(this NetDaemonRxApp app)
    {
        var people = app.States.Where(e => e.EntityId.StartsWith("person."));

        var peopleHome = people.Count(p => p.State == "home");

        var bedOccupancyCount = app.State("sensor.bed_occupancy_count")!.State;

        return peopleHome == bedOccupancyCount;
    }
}