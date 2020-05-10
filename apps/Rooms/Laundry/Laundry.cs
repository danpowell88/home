using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EnumsNET;
using JetBrains.Annotations;
using JoySoftware.HomeAssistant.NetDaemon.Common;

[UsedImplicitly]
public class Laundry : RoomApp
{
    protected override bool IndoorRoom => true;
    protected override TimeSpan OccupancyTimeout => TimeSpan.FromMinutes(5);

    private readonly Func<IEntityProperties, bool> _washingMachineStatus = e => e.EntityId == "input_select.washing_machine_status";
    private readonly Func<IEntityProperties, bool> _washingMachinePowerSensor = e => e.EntityId == "switch.washing_machine";
    private readonly Func<IEntityProperties, bool> _washingMachineDoor = e => e.EntityId == "binary_sensor.dishwasher_door_contact";

    public override Task InitializeAsync()
    {
        Entities(_washingMachinePowerSensor)
            .WhenStateChange((to, from) =>
            {
                var resetStates = new List<WashingMachineState> { WashingMachineState.Idle, WashingMachineState.Clean, WashingMachineState.Finishing };

                return GetWashingMachineWattage(to!) > 10D &&
                       resetStates.Contains(GetWashingMachineState());
            })
            .Call(async (_, __, ___) =>
                await InputSelects(_washingMachineStatus).SetOption(WashingMachineState.Running).ExecuteAsync())
            .Execute();

        Entities(_washingMachinePowerSensor)
            .WhenStateChange((to, from) =>
                GetWashingMachineWattage(to!) < 6D &&
                GetWashingMachineState() == WashingMachineState.Running)
            .Call(async (_, __, ___) =>
                await InputSelects(_washingMachineStatus).SetOption(WashingMachineState.Finishing).ExecuteAsync())
            .Execute();

        Entities(_washingMachineStatus)
            .WhenStateChange((to, from) =>
                GetWashingMachineState() == WashingMachineState.Finishing)
            .AndNotChangeFor(new TimeSpan(0,2,0))
            .Call(async (_, __, ___) =>
                await InputSelects(_washingMachineStatus).SetOption(WashingMachineState.Clean).ExecuteAsync())
            .Execute();

        Entities(_washingMachineDoor)
            .WhenStateChange((to, from) =>
                from!.State == "off" &&
                to!.State == "on" &&
                GetWashingMachineState() == WashingMachineState.Clean)
            .Call(async (_, __, ___) =>
                await InputSelects(_washingMachineStatus).SetOption(WashingMachineState.Idle).ExecuteAsync())
            .Execute();

        Entities(_washingMachineStatus)
            .WhenStateChange((to, from) =>
                from!.State == WashingMachineState.Running.ToString("F") &&
                to!.State == WashingMachineState.Clean.ToString("F"))
            .Call(async (_, __, ___) =>
                await this.Notify(
                    "Laundry",
                    "The washing machine has finished",
                    Notifier.NotificationCriteria.Always,
                    Notifier.NotificationCriteria.Home,
                    Notifier.TextNotificationDevice.All))
            .Execute();

        return base.InitializeAsync();
    }

    private static double GetWashingMachineWattage(EntityState to)
    {
        return to!.Attribute!.current_power_w ?? 0D;
    }

    private WashingMachineState GetWashingMachineState()
    {
        return Enums.Parse<WashingMachineState>(State.Single(_washingMachineStatus).State);
    }

    private enum WashingMachineState
    {
        Idle,
        Running,
        Finishing,
        Clean
    }
}