using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using daemonapp.Utilities;
using EnumsNET;
using JetBrains.Annotations;
using NetDaemon.Common;
using NetDaemon.Common.Reactive;

[UsedImplicitly]
public class Kitchen : RoomApp
{
    protected override bool IndoorRoom => true;
    protected override TimeSpan OccupancyTimeout => TimeSpan.FromMinutes(10);

    private const string DishwasherStatus = "input_select.dishwasher_status";
    private const string DishwasherPowerSensor = "switch.dishwasher";
    private const string DishwasherDoor = "binary_sensor.dishwasher_door_contact";

    protected override bool SecondaryLightingEnabled => DateTime.Now.Hour >= 18 && DateTime.Now.Hour <= 22;

    public override void Initialize()
    {
        SetupDishwasher();

        //Entity("binary_sensor.fridge_door_contact")
        //    .StateChangesFiltered()
        //    .Where(s => s.Old!.State == "off" && s.New.State == "on")
        //    .Subscribe(_ =>
        //    {
        //        if ((State("person.daniel")!.State != "home" ||
        //             State("binary_sensor.media_chair_right_occupancy")!.State == "on") &&
        //            DateTime.Now.Hour > 15 && DateTime.Now.Hour < 20 &&
        //            ((DateTime?)Storage.LastFridgeNotification == null ||
        //             DateTime.Now - (DateTime)Storage.LastFridgeNotification >=
        //             TimeSpan.FromHours(24)))
        //        {
        //            this.Notify(new Uri("http://192.168.1.2:8123/local/big_pig_snort.mp3"), 0.4M,
        //                Notifier.AudioNotificationDevice.Kitchen);
        //            Storage.LastFridgeNotification = DateTime.Now;
        //        }
        //    });


        base.Initialize();
    }

    private void SetupDishwasher()
    {
        Entity(DishwasherPowerSensor)
            .StateAllChangesFiltered()
            .Where(s =>
            {
                var resetStates = new List<DishwasherState> { DishwasherState.Dirty, DishwasherState.Clean };

                return GetDishwasherWattage(s.New!) > 10D &&
                       resetStates.Contains(GetDishwasherState());
            })
            .Subscribe(_ =>
            {
                LogHistory("Dishwasher running");
                Entity(DishwasherStatus).SetOption(DishwasherState.Running);
            });

        Entity(DishwasherPowerSensor)
            .StateAllChangesFiltered()
            .Where(s =>
                GetDishwasherWattage(s.New!) < 1D &&
                GetDishwasherState() == DishwasherState.Running)
            .NDSameStateFor(new TimeSpan(0, 1, 45))
            .Subscribe(_ =>
            {
                LogHistory("Dishwasher clean");
                Entity(DishwasherStatus).SetOption(DishwasherState.Clean);
            });

        Entity(DishwasherDoor)
            .StateChangesFiltered()
            .Where(s =>
                s.Old!.State == "off" &&
                s.New!.State == "on" &&
                (
                    GetDishwasherWattage(s.New!) < 1D ||
                    GetDishwasherState() == DishwasherState.Clean)
            )
            .Subscribe(_ =>
            {
                LogHistory("Dishwasher dirty");
                Entity(DishwasherStatus).SetOption(DishwasherState.Dirty);
            });

        Entity(DishwasherStatus)
            .StateChangesFiltered()
            .Where(s => s.Old!.State == DishwasherState.Running.ToString("F") &&
                        s.New!.State == DishwasherState.Clean.ToString("F"))
            .Subscribe(_ =>
            {

                LogHistory("Dishwasher finished notification");
                this.Notify(
                    "Kitchen",
                    "The dishwasher has finished",
                    Notifier.NotificationCriteria.NotSleeping,
                    Notifier.NotificationCriteria.NotSleeping,
                    Notifier.TextNotificationDevice.All);
            });
    }

    private static double GetDishwasherWattage(EntityState to)
    {
        return to!.Attribute!.current_power_w ?? 0D;
    }

    private DishwasherState GetDishwasherState()
    {
        return Enums.Parse<DishwasherState>(State(DishwasherStatus)!.State);
    }

    private enum DishwasherState
    {
        Dirty,
        Running,
        Clean
    }
}