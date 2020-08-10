using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EnumsNET;
using JetBrains.Annotations;

using NetDaemon.Common;
using NetDaemon.Common.Fluent;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

[UsedImplicitly]
public class Laundry : RoomApp
{
    protected override bool IndoorRoom => true;
    protected override TimeSpan OccupancyTimeout => TimeSpan.FromMinutes(5);

    private readonly Func<IEntityProperties, bool> _washingMachineStatus = e => e.EntityId == "input_select.washing_machine_status";
    private readonly Func<IEntityProperties, bool> _washingMachinePowerSensor = e => e.EntityId == "switch.washing_machine";
    private readonly Func<IEntityProperties, bool> _washingMachineDoor = e => e.EntityId == "binary_sensor.dishwasher_door_contact";

    private ISchedulerResult? _washingDoneTimer;

    public override void Initialize()
    {
        //Entities(_washingMachinePowerSensor)
        //    .WhenStateChange((to, from) =>
        //    {
        //        var resetStates = new List<WashingMachineState>
        //            {WashingMachineState.Idle, WashingMachineState.Clean, WashingMachineState.Finishing};

        //        return GetWashingMachineWattage(to!) > 10D &&
        //               resetStates.Contains(GetWashingMachineState());
        //    })
        //    .Call(async (_, __, ___) => {
        //        CancelWashingDoneTimer();
        //        await InputSelects(_washingMachineStatus).SetOption(WashingMachineState.Running).ExecuteAsync();
        //    })
        //    .Execute();

        //Entities(_washingMachinePowerSensor)
        //    .WhenStateChange((to, from) =>
        //        GetWashingMachineWattage(to!) < 6D &&
        //        GetWashingMachineState() == WashingMachineState.Running)
        //    .AndNotChangeFor(TimeSpan.FromSeconds(170))
        //    .Call(async (_, __, ___) =>
        //        await InputSelects(_washingMachineStatus).SetOption(WashingMachineState.Finishing).ExecuteAsync())
        //    .Execute();

        //Entities(_washingMachineStatus)
        //    .WhenStateChange((to, from) =>
        //        GetWashingMachineState() == WashingMachineState.Finishing)
        //    .AndNotChangeFor(new TimeSpan(0, 1, 0))
        //    .Call(async (_, __, ___) =>
        //        await InputSelects(_washingMachineStatus).SetOption(WashingMachineState.Clean).ExecuteAsync())
        //    .Execute();

        //Entities(_washingMachineDoor)
        //    .WhenStateChange((to, from) =>
        //        from!.State == "off" &&
        //        to!.State == "on" &&
        //        GetWashingMachineState() != WashingMachineState.Running)
        //    .Call(async (_, __, ___) =>
        //    {
        //        CancelWashingDoneTimer();
        //        await InputSelects(_washingMachineStatus).SetOption(WashingMachineState.Idle).ExecuteAsync();
        //    })
        //    .Execute();

        //Entities(_washingMachineStatus)
        //    .WhenStateChange((to, from) => 
        //        from!.State == WashingMachineState.Finishing.ToString("F") &&
        //        to!.State == WashingMachineState.Clean.ToString("F"))
        //    .Call(async (_, __, ___) =>
        //    {
        //        Log(LogLevel.Information, "washing state changed");
                
        //        if (_washingDoneTimer != null)
        //            return;

        //        _washingDoneTimer = Scheduler.RunEvery(TimeSpan.FromMinutes(30), async () =>
        //        {
        //            if (GetWashingMachineState() == WashingMachineState.Clean)
        //            {
        //                Log(LogLevel.Information, "about to notify washing machine");
        //                await this.Notify(
        //                    "Laundry",
        //                    "The washing machine has finished",
        //                    Notifier.NotificationCriteria.Always,
        //                    Notifier.NotificationCriteria.Always,
        //                    new[]
        //                    {
        //                        new Notifier.NotificationAction("silence_washingdone", "Silence")

        //                    },
        //                    Notifier.TextNotificationDevice.All);
        //            }
        //        });

        //        await Task.CompletedTask;
        //    })
        //    .Execute();

        //Events(e => e.EventId == "mobile_app_notification_action" && e.Data!.action == "silence_washingdone")
        //    .Call(async (_, __) =>
        //    {
        //        CancelWashingDoneTimer();
        //        await Task.CompletedTask;
        //    })
        //    .Execute();

        base.Initialize();
    }

    //private void CancelWashingDoneTimer()
    //{
    //    if (_washingDoneTimer != null)
    //    {
    //        _washingDoneTimer.CancelSource.Cancel();
    //        _washingDoneTimer.CancelSource.Dispose();
    //        _washingDoneTimer = null;
    //    }
    //}

    //private static double GetWashingMachineWattage(EntityState to)
    //{
    //    return to!.Attribute!.current_power_w ?? 0D;
    //}

    //private WashingMachineState GetWashingMachineState()
    //{
    //    return Enums.Parse<WashingMachineState>(State.Single(_washingMachineStatus).State);
    //}

    //private enum WashingMachineState
    //{
    //    Idle,
    //    Running,
    //    Finishing,
    //    Clean
    //}
}