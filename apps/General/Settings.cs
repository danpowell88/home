using System.Threading.Tasks;
using JoySoftware.HomeAssistant.NetDaemon.Common;

public class Settings : NetDaemonApp
{
    public override Task InitializeAsync()
    {
        Entity("sun.sun")
            .WhenStateChange((to, from) =>
                from!.Attribute!.elevation != to!.Attribute!.elevation)
            .Call(SetMotionVariables)
            .Execute();

        Entity("input_boolean.left_home")
            .WhenStateChange(from: "off", to: "on")
            .UseEntity("input_boolean.indoor_motion_enabled").TurnOff()
            .Execute();

        Entity("group.family")
            .WhenStateChange(from: "not_home", to: "home")
            .UseEntity("input_boolean.indoor_motion_enabled").TurnOn()
            .Execute();

        return base.InitializeAsync();
    }

    private async Task SetMotionVariables(string entityId, EntityState? to, EntityState? from)
    {
        if (from!.Attribute!.elevation < 5 && to!.Attribute!.elevation <= 5)
        {
            await Entity("input_boolean.outdoor_motion_enabled").TurnOn().ExecuteAsync();
        }
        else
        {
            await Entity("input_boolean.outdoor_motion_enabled").TurnOff().ExecuteAsync();
        }
    }
}