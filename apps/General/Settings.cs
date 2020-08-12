using System;
using System.Linq;
using System.Reactive.Linq;
using NetDaemon.Common;
using NetDaemon.Common.Reactive;

public class Settings : NetDaemonRxApp
{
    public override void Initialize()
    {
        Entity("sun.sun")
            .StateAllChanges
            .Where(s => s.Old.Attribute!.elevation != s.New.Attribute!.elevation)
            .Subscribe(s => SetMotionVariables(s.New));

        Entity("input_boolean.left_home")
            .StateChangesFiltered()
            .Where(s => s.Old.State == "off" && s.New.State == "on")
            .Subscribe(_ => Entity("input_boolean.indoor_motion_enabled").TurnOff());

        Entity("sensor.bed_occupancy_count")
            .StateChangesFiltered()
            .Subscribe(_ =>
            {
                if (this.IsEveryoneInBed())
                {
                    Entity("input_boolean.indoor_motion_enabled").TurnOff();
                }
                else if(State("group.family")!.State == "home")
                {
                    Entity("input_boolean.indoor_motion_enabled").TurnOn();
                }
            });

        Entity("group.family")
            .StateChangesFiltered()
            .Where(s => s.Old.State == "not_home" && s.New.State == "home")
            .Subscribe(_ => Entity("input_boolean.indoor_motion_enabled").TurnOn());

        Entity("input_boolean.party_mode")
            .StateChangesFiltered()
            .Where(s => s.Old.State == "off" && s.New.State == "on")
            .NDSameStateFor(TimeSpan.FromHours(6))
            .Subscribe(_ => Entity("input_boolean.party_mode").TurnOff());

        Entities(e => e.EntityId.StartsWith("light."))
            .StateChangesFiltered()
            .Subscribe(_ =>
            {
                if (States.Any(e => e.EntityId.StartsWith("light.") && e.State == "on"))
                {
                    Entity("input_boolean.any_light_on").TurnOn();
                }
                else
                {
                    Entity("input_boolean.any_light_on").TurnOff();
                }
            });


        RunDaily("01:00:00", () => Entity("input_boolean.party_mode").TurnOff());

        base.Initialize();
    }

    private void SetMotionVariables(EntityState to)
    {
        if (to.Attribute!.elevation <= 2 && to!.Attribute!.rising == false ||
            to!.Attribute!.elevation <= -12.0 && to!.Attribute!.rising == true)
        {
             Entity("input_boolean.outdoor_motion_enabled").TurnOn();
        }
        else
        {
             Entity("input_boolean.outdoor_motion_enabled").TurnOff();
        }
    }
}