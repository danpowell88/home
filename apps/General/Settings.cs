using System.Threading.Tasks;
using JoySoftware.HomeAssistant.NetDaemon.Common;

public class Settings : NetDaemonApp
{
    public override Task InitializeAsync()
    {
        Entity("sun.sun")
            .WhenStateChange((to, from) =>
                from!.Attribute!.elevation > 5L &&
                to!.Attribute!.elevation <= 5L &&
                to!.Attribute!.rising == false
            ).UseEntity("input_boolean.outdoor_motion_enabled").TurnOn().Execute();

        Entity("sun.sun")
            .WhenStateChange((to, from) =>
                from!.Attribute!.elevation < 5L &&
                to!.Attribute!.elevation >= 5L &&
                to!.Attribute!.rising == false
            ).UseEntity("input_boolean.outdoor_motion_enabled").TurnOff().Execute();

        return base.InitializeAsync();
    }
}