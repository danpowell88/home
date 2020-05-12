using System.Threading.Tasks;
using JoySoftware.HomeAssistant.NetDaemon.Common;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

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
        // 5pm - 5am
        //  1.72 | -17.7

        Log(LogLevel.Information, "elevation: {elevation} rising:{rising}", to.Attribute.elevation, to.Attribute.rising);

        Log(LogLevel.Information, "< 1.72 {result}", to!.Attribute!.elevation <= 1.72);
        Log(LogLevel.Information, ">= -17.7 {result}", to!.Attribute!.elevation >= -17.7);
        Log(LogLevel.Information, "rising {result} {type}", to!.Attribute!.rising == false, to!.Attribute.rising.GetType());

        if ((to!.Attribute!.elevation <= 1.72 &&  to!.Attribute!.rising == false) ||
            to!.Attribute!.elevation >= -17.7 && to!.Attribute!.rising == true)
        {
            Log(LogLevel.Information, "outdoor motion enabled");
            await Entity("input_boolean.outdoor_motion_enabled").TurnOn().ExecuteAsync();
        }
        else
        {
            Log(LogLevel.Information, "outdoor motion disabled");
            await Entity("input_boolean.outdoor_motion_enabled").TurnOff().ExecuteAsync();
        }
    }
}