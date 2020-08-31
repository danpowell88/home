using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using daemonapp.Utilities;
using EnumsNET;
using JetBrains.Annotations;
using NetDaemon.Common.Reactive;

[UsedImplicitly]
public class Kitchen : RoomApp
{
    protected override bool IndoorRoom => true;
    protected override TimeSpan OccupancyTimeout => TimeSpan.FromMinutes(10);

    private const string DishwasherStatus = "input_select.dishwasher_status";
    private const string DishwasherPowerSensor = "sensor.dishwasher_watts";
    private const string DishwasherDoor = "binary_sensor.dishwasher_door_contact";

    protected override bool SecondaryLightingEnabled => DateTime.Now.Hour >= 18 && DateTime.Now.Hour <= 22;

    public override void Initialize()
    {
        SetupDishwasher();

        base.Initialize();
    }

    private void SetupDishwasher()
    {
        Entity(DishwasherPowerSensor)
            .StateAllChangesFiltered()
            .FilterDistinctUntilChanged(s =>
            {
                var resetStates = new List<DishwasherState> { DishwasherState.Dirty, DishwasherState.Clean };

                return GetDishwasherWattage() > 10D &&
                       resetStates.Contains(GetDishwasherState());
            })
            .Subscribe(_ =>
            {
                LogHistory("Dishwasher running");
                Entity(DishwasherStatus).SetOption(DishwasherState.Running);
            });

        Entity(DishwasherPowerSensor)
            .StateChangesFiltered()
            .FilterDistinctUntilChanged(s =>
                GetDishwasherWattage() < 1D &&
                GetDishwasherState() == DishwasherState.Running)
            .NDSameStateFor(new TimeSpan(0, 1, 45))
            .Subscribe(_ =>
            {
                LogHistory("Dishwasher clean");
                Entity(DishwasherStatus).SetOption(DishwasherState.Clean);
            });

        Entity(DishwasherDoor)
            .StateChangesFiltered()
            .FilterDistinctUntilChanged(s =>
                s.Old!.State == "off" &&
                s.New!.State == "on" &&
                (
                    GetDishwasherWattage() < 1D ||
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

    private double GetDishwasherWattage()
    {
        return State(DishwasherPowerSensor)?.State ?? 0;
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