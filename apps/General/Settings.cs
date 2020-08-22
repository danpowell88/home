using System;
using System.Linq;
using System.Reactive.Linq;
using daemonapp.Utilities;
using NetDaemon.Common;
using NetDaemon.Common.Reactive;

public class Settings : NetDaemonRxApp
{
    public override void Initialize()
    {
        Entity("sun.sun")
            .StateAllChanges
            .Where(s => s.Old.Attribute!.elevation != s.New.Attribute!.elevation)
            .Subscribe(s => SetOutdoorMotionVariables(s.New));

        Entity("input_boolean.left_home")
            .StateChangesFiltered()
            .Where(s => s.Old.State == "off" && s.New.State == "on")
            .Subscribe(_ => SetIndoorMotionVariables());

        Entity("sensor.bed_occupancy_count")
            .StateChangesFiltered()
            .Synchronize()
            .Subscribe(_ => { SetIndoorMotionVariables(); });

        Entity("group.family")
            .StateChangesFiltered()
            .Synchronize()
            //.Where(s => s.Old.State == "not_home" && s.New.State == "home")
            .Subscribe(_ => SetIndoorMotionVariables());

        Entity("input_boolean.party_mode")
            .StateChangesFiltered()
            .Where(s => s.Old.State == "off" && s.New.State == "on")
            .NDSameStateFor(TimeSpan.FromHours(6))
            .Subscribe(_ => Entity("input_boolean.party_mode").TurnOff());

        Entities(e => e.EntityId.StartsWith("light."))
            .StateChangesFiltered()
            .Synchronize()
            .Subscribe(_ => { SetLightState(); });


        RunDaily("01:00:00", () => Entity("input_boolean.party_mode").TurnOff());


        SetLightState();
        SetIndoorMotionVariables();
        SetOutdoorMotionVariables(State("sun.sun")!);

        base.Initialize();
    }

    private void SetIndoorMotionVariables()
    {
        if (this.IsEveryoneInBed())
        {
            Entity("input_boolean.indoor_motion_enabled").TurnOff();
        }
        else if (State("group.family")!.State == "home")
        {
            Entity("input_boolean.indoor_motion_enabled").TurnOn();
        }
    }

    private void SetLightState()
    {
        if (States.Any(e => e.EntityId.StartsWith("light.") && e.State == "on"))
        {
            Entity("input_boolean.any_light_on").TurnOn();
        }
        else
        {
            Entity("input_boolean.any_light_on").TurnOff();
        }
    }

    private void SetOutdoorMotionVariables(EntityState to)
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