using System;
using System.Reactive.Linq;
using JetBrains.Annotations;
using NetDaemon.Common.Reactive;

[UsedImplicitly]
public class Pantry : RoomApp
{
    protected override bool IndoorRoom => true;
    protected override TimeSpan OccupancyTimeout => TimeSpan.FromMinutes(2);

    private IDisposable? _fridgeOpenTimer;

    public override void Initialize()
    {
        Entity("binary_sensor.fridge_door_contact")
            .StateChangesFiltered()
            .Where(s => s.Old.State == "off" && s.New.State == "on")
            .NDSameStateFor(TimeSpan.FromMinutes(3))
            .Subscribe(_ =>
            {
                CancelOpenTimer();

                _fridgeOpenTimer = RunEvery(TimeSpan.FromMinutes(3), () =>
                {
                    if (State("binary_sensor.fridge_door_contact")!.State == "on")
                    {
                        this.Notify(
                            "Pantry",
                            $"The fridge door has been left open",
                            Notifier.NotificationCriteria.Home,
                            Notifier.NotificationCriteria.None,
                            new[]
                            {
                                new Notifier.NotificationAction("silence_fridge", "Silence")
                            },
                            Notifier.TextNotificationDevice.All);
                    }
                });
            });

        Entity("binary_sensor.fridge_door_contact")
            .StateChangesFiltered()
            .Where(s => s.Old.State == "off" && s.New.State == "on")
            .Subscribe(_ => CancelOpenTimer());

        EventChanges.Where(e => e.Event == "mobile_app_notification_action" && e.Data!.action == "silence_fridge")
            .Subscribe(_ => CancelOpenTimer());

        base.Initialize();
    }

    private void CancelOpenTimer()
    {
        if (_fridgeOpenTimer != null)
        {
            _fridgeOpenTimer.Dispose();
            _fridgeOpenTimer = null;
        }
    }
}