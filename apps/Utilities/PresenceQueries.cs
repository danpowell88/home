using System.Linq;
using JoySoftware.HomeAssistant.NetDaemon.Common;

public static class PresenceQueries
{
    public static bool IsAnyoneHome(this NetDaemonApp app)
    {
        return app.State.Single(e => e.EntityId == "group.family").State == "home";
    }

    public static bool IsAnyoneSleeping(this NetDaemonApp app)
    {
        // todo: bayesian sensor to detect either sleeping
        return app.State.Single(e => e.EntityId == "binary_sensor.bed_occupancy").State == "on";
    }

    public static bool IsAnyoneInBed(this NetDaemonApp app)
    {
        return app.State.Single(e => e.EntityId == "binary_sensor.bed_occupancy").State == "on";
    }

    public static bool IsEveryoneInBed(this NetDaemonApp app)
    {
        var people = app.State.Where(e => e.EntityId.StartsWith("person."));

        var peopleHome = people.Count(p => p.State == "home");

        var bedOccupancyCount = app.State.Single(e => e.EntityId == "sensor.bed_occupancy_count").State;

        return peopleHome == bedOccupancyCount;
    }
}