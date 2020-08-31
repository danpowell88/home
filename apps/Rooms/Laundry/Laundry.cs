using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using daemonapp.Utilities;
using EnumsNET;
using JetBrains.Annotations;
using NetDaemon.Common.Reactive;

[UsedImplicitly]
public class Laundry : RoomApp
{
    protected override bool IndoorRoom => true;
    protected override TimeSpan OccupancyTimeout => TimeSpan.FromMinutes(5);

    private const string WashingMachineStatus = "input_select.washing_machine_status";
    private const string WashingMachinePowerSensor = "sensor.washing_machine_watts";
    private const string WashingMachineDoorContact = "binary_sensor.washing_machine_door_contact";

    private IDisposable? _washingDoneTimer;

    public override void Initialize()
    {
        Entities(WashingMachinePowerSensor, WashingMachineStatus)
            .StateChangesFiltered()
            .FilterDistinctUntilChanged(s =>
            {
                var resetStates = new List<WashingMachineState>
                    {WashingMachineState.Idle, WashingMachineState.Clean, WashingMachineState.Finishing};

                return GetWashingMachineWattage() > 10D &&
                       resetStates.Contains(GetWashingMachineState());
            })
            .Subscribe(_ =>
            {
                LogHistory("Washing machine running");
                CancelWashingDoneTimer();

                Entity(WashingMachineStatus).SetOption(WashingMachineState.Running);
            });

        Entities(WashingMachinePowerSensor, WashingMachineStatus)
            .StateChangesFiltered()
            .FilterDistinctUntilChanged(s => GetWashingMachineWattage() < 5D && GetWashingMachineState() == WashingMachineState.Running)
            .NDSameStateFor(new TimeSpan(0, 1, 0))
            .Subscribe(_ =>
            {
                LogHistory("Washing machine finishing");
                Entity(WashingMachineStatus).SetOption(WashingMachineState.Finishing);
            });

        Entities(WashingMachineStatus)
            .StateChangesFiltered()
            .Where(s => GetWashingMachineState() == WashingMachineState.Finishing)
            .NDSameStateFor(new TimeSpan(0, 1, 0))
            .Subscribe(_ =>
            {
                LogHistory("Washing machine clean");
                Entity(WashingMachineStatus).SetOption(WashingMachineState.Clean);
            });

        Entities(WashingMachineDoorContact)
            .StateChangesFiltered()
            .Where(s =>
                s.Old!.State == "off" &&
                s.New!.State == "on" &&
                GetWashingMachineState() != WashingMachineState.Running)
            .Subscribe(_ =>
            {
                LogHistory("Washing machine idle");
                CancelWashingDoneTimer();
                Entity(WashingMachineStatus).SetOption(WashingMachineState.Idle);
            });

        Entities(WashingMachineStatus)
            .StateChangesFiltered()
            .Where(s =>
                s.Old!.State == WashingMachineState.Finishing.ToString("F") &&
                s.New!.State == WashingMachineState.Clean.ToString("F"))
            .Subscribe(_ =>
            {
                LogHistory("Washing machine clean notification");

                if (_washingDoneTimer != null)
                    return;

                _washingDoneTimer = RunNowAndEvery(TimeSpan.FromMinutes(30), () =>
                {
                    if (GetWashingMachineState() == WashingMachineState.Clean)
                    {
                        this.Notify(
                            "Laundry",
                            "The washing machine has finished",
                            Notifier.NotificationCriteria.Always,
                            Notifier.NotificationCriteria.Always,
                            new[] {new Notifier.NotificationAction("silence_washingdone", "Silence")},
                            Notifier.TextNotificationDevice.All);
                    }
                });

                Entity(WashingMachineStatus).SetOption(WashingMachineState.Clean);
            });

        EventChanges.Where(e => e.Event == "mobile_app_notification_action" && e.Data!.action == "silence_washingdone")
            .Subscribe(_ =>
            {
                LogHistory("Washing machine silence notification");
                CancelWashingDoneTimer();
            });

        base.Initialize();
    }

    private void CancelWashingDoneTimer()
    {
        if (_washingDoneTimer != null)
        {
            _washingDoneTimer.Dispose();
            _washingDoneTimer = null;
        }
    }

    private double GetWashingMachineWattage()
    {
        return State(WashingMachinePowerSensor)?.State ?? 0;
    }

    private WashingMachineState GetWashingMachineState()
    {
        return Enums.Parse<WashingMachineState>(State(WashingMachineStatus)!.State);
    }

    private enum WashingMachineState
    {
        Idle,
        Running,
        Finishing,
        Clean
    }
}