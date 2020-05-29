using System;
using System.Linq;
using System.Threading.Tasks;
using JoySoftware.HomeAssistant.NetDaemon.Common;
using Microsoft.CodeAnalysis.CSharp.Syntax;

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

        Entity("sensor.bed_occupancy_count")
            .WhenStateChange()
            .Call(async (_, __, ___) =>
            {
                if (this.IsEveryoneInBed())
                {
                    await Entity("input_boolean.indoor_motion_enabled").TurnOff().ExecuteAsync();
                }
                else if(GetState("group.family")!.State == "home")
                {
                    await Entity("input_boolean.indoor_motion_enabled").TurnOn().ExecuteAsync();
                }
            }).Execute();

        Entity("group.family")
            .WhenStateChange(from: "not_home", to: "home")
            .UseEntity("input_boolean.indoor_motion_enabled").TurnOn()
            .Execute();

        Entity("input_boolean.party_mode")
            .WhenStateChange(from: "off", to: "on")
            .AndNotChangeFor(TimeSpan.FromHours(6))
            .UseEntity("input_boolean.party_mode")
            .TurnOff()
            .Execute();

        Entities(e => e.EntityId.StartsWith("light."))
            .WhenStateChange()
            .Call(async (_, __, ___) =>
            {
                if (State.Any(e => e.EntityId.StartsWith("light.") && e.State == "on"))
                {
                    await Entity("input_boolean.any_light_on").TurnOn().ExecuteAsync();
                }
                else
                {
                    await Entity("input_boolean.any_light_on").TurnOff().ExecuteAsync();
                }
            })
            .Execute();

        Scheduler.RunDaily("01:00:00", async () => await Entity("input_boolean.party_mode").TurnOff().ExecuteAsync());

        return base.InitializeAsync();
    }

    private async Task SetMotionVariables(string entityId, EntityState? to, EntityState? from)
    {
        if ((to!.Attribute!.elevation <= 2 &&  to!.Attribute!.rising == false) ||
            to!.Attribute!.elevation <= -12.0 && to!.Attribute!.rising == true)
        {
            await Entity("input_boolean.outdoor_motion_enabled").TurnOn().ExecuteAsync();
        }
        else
        {
            await Entity("input_boolean.outdoor_motion_enabled").TurnOff().ExecuteAsync();
        }
    }
}