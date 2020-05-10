using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EnumsNET;
using JetBrains.Annotations;
using JoySoftware.HomeAssistant.NetDaemon.Common;

[UsedImplicitly]
public class Kitchen : RoomApp
{
    protected override bool IndoorRoom => true;
    protected override TimeSpan OccupancyTimeout => TimeSpan.FromMinutes(10);

    private readonly Func<IEntityProperties, bool> _dishwasherStatus = e => e.EntityId == "input_select.dishwasher_status";
    private readonly Func<IEntityProperties, bool> _dishwasherPowerSensor = e => e.EntityId == "switch.dishwasher";
    private readonly Func<IEntityProperties, bool> _dishwasherDoor = e => e.EntityId == "binary_sensor.dishwasher_door_contact";

    public override Task InitializeAsync()
    {
        Entities(_dishwasherPowerSensor)
            .WhenStateChange((to, from) =>
            {
                var resetStates = new List<DishwasherState> {DishwasherState.Dirty, DishwasherState.Clean};

                return GetDishwasherWattage(to!) > 10D &&
                       resetStates.Contains(GetDishwasherState());
            })
            .Call(async (_, __, ___) =>
                await InputSelects(_dishwasherStatus).SetOption(DishwasherState.Running).ExecuteAsync())
            .Execute();

        Entities(_dishwasherPowerSensor)
            .WhenStateChange((to, from) => 
                GetDishwasherWattage(to!) < 1D && 
                GetDishwasherState() == DishwasherState.Running)
            .Call(async (_, __, ___) =>
                await InputSelects(_dishwasherStatus).SetOption(DishwasherState.Clean).ExecuteAsync())
            .Execute();

        Entities(_dishwasherDoor)
            .WhenStateChange((to, from) =>
                from!.State == "off" && 
                to!.State == "on" && 
                (
                    GetDishwasherWattage(to!) < 1D || 
                    GetDishwasherState() == DishwasherState.Clean)
                )
            .Call(async (_, __, ___) =>
                await InputSelects(_dishwasherStatus).SetOption(DishwasherState.Dirty).ExecuteAsync())
            .Execute();

        Entities(_dishwasherStatus)
            .WhenStateChange((to, from) => 
                from!.State == DishwasherState.Running.ToString("F") && 
                to!.State == DishwasherState.Clean.ToString("F"))
            .Call(async (_, __, ___) =>
                await this.Notify(
                    "Kitchen", 
                    "The dishwasher has finished",
                    Notifier.NotificationCriteria.Home,
                    Notifier.NotificationCriteria.NotSleeping,
                    Notifier.TextNotificationDevice.All))
            .Execute();

        return base.InitializeAsync();
    }

    private static double GetDishwasherWattage(EntityState to)
    {
        return to!.Attribute!.current_power_w ?? 0D;
    }

    private DishwasherState GetDishwasherState()
    {
        return Enums.Parse<DishwasherState>(State.Single(_dishwasherStatus).State);
    }

    private enum DishwasherState
    {
        Dirty,
        Running,
        Clean
    }
}