using System;
using System.Threading.Tasks;
using JetBrains.Annotations;
using JoySoftware.HomeAssistant.NetDaemon.Common;

[UsedImplicitly]
public class Pantry : RoomApp
{
    protected override bool IndoorRoom => true;
    protected override TimeSpan OccupancyTimeout => TimeSpan.FromMinutes(2);

    private ISchedulerResult? _fridgeOpenTimer;

    public override Task InitializeAsync()
    {
        Entity("binary_sensor.fridge_door_contact")
            .WhenStateChange(from: "off", to: "on")
            .AndNotChangeFor(TimeSpan.FromMinutes(3))
        .Call(
            async (_, __, ___) =>
            {
                CancelOpenTimer();

                _fridgeOpenTimer = Scheduler.RunEvery(TimeSpan.FromMinutes(3), async () =>
                {
                    if (GetState("binary_sensor.fridge_door_contact")!.State == "off")
                    {
                        await this.Notify(
                            "Pantry",
                            $"The fridge door has been left open",
                            Notifier.NotificationCriteria.Home,
                            Notifier.NotificationCriteria.Home,
                            new[]
                            {
                                new Notifier.NotificationAction ("silence_fridge", "Silence")
                            },
                            Notifier.TextNotificationDevice.All);
                    }
                });

                await Task.CompletedTask;
            }).Execute();

        Events(e => e.EventId == "mobile_app_notification_action" && e.Data!.action == "silence_fridge")
            .Call(async (_, __) =>
            {
                CancelOpenTimer();
                await Task.CompletedTask;
            }).Execute();

        return base.InitializeAsync();
    }

    private void CancelOpenTimer()
    {
        if (_fridgeOpenTimer != null)
        {
            _fridgeOpenTimer.CancelSource.Cancel();
            _fridgeOpenTimer = null;
        }
    }
}