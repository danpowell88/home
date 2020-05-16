using System;
using System.Threading.Tasks;
using JetBrains.Annotations;
using JoySoftware.HomeAssistant.NetDaemon.Common;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

[UsedImplicitly]
public class Garage : RoomApp
{
    protected override bool IndoorRoom => true;
    protected override TimeSpan OccupancyTimeout => TimeSpan.FromMinutes(10);

    private ISchedulerResult? _garageOpenTimer;

    public override Task InitializeAsync()
    {
        Entity("cover.garage_door")
            .WhenStateChange(from: "closed", to: "open")
            .AndNotChangeFor(TimeSpan.FromMinutes(20))
        .Call(
            async (_, __, ___) =>
            {
               _garageOpenTimer = Scheduler.RunEvery(TimeSpan.FromMinutes(20), async () =>
               {
                    if (GetState("cover.garage_door")!.State == "open")
                    {
                        var filename = $"{Guid.NewGuid()}.jpg";

                        await CallService("camera", "snapshot", new
                        {
                            filename = $"www/snapshots/tmp/{filename}",
                            entity_id = "camera.garage"
                        }, true);

                        await this.Notify(
                            "Security",
                            $"The garage door has been left open",
                            Notifier.NotificationCriteria.Always,
                            Notifier.NotificationCriteria.Home,
                            new[]
                            {
                                new Notifier.NotificationAction ("close_garage", "Close"),
                                new Notifier.NotificationAction ("silence_garage", "Silence")
                            },
                            $"https://home.danielpowell.net/local/snapshots/tmp/{filename}",
                            Notifier.TextNotificationDevice.All);
                    }
                });

                await Task.CompletedTask;
            }).Execute();

        Events(e => e.EventId == "mobile_app_notification_action" && e.Data!.action == "close_garage")
            .Call(async (_, __) =>
            {
                if (GetState("cover.garage_door")!.State == "open")
                {
                    await CallService("cover", "close_cover", new {entity_id = "cover.garage_door"});
                }
            }).Execute();

        Events(e => e.EventId == "mobile_app_notification_action" && e.Data!.action == "silence_garage")
            .Call(async (_, __) =>
            {
                CancelOpenTimer();
                await Task.CompletedTask;
            }).Execute();

        Entity("cover.garage_door")
            .WhenStateChange(from: "open", to: "closed").Call(async (_, __, ___) =>
            {
                CancelOpenTimer();
                await Task.CompletedTask;
            }).Execute();

        return base.InitializeAsync();
    }

    private void CancelOpenTimer()
    {
        if (_garageOpenTimer != null)
        {
            _garageOpenTimer.CancelSource.Cancel();
            _garageOpenTimer = null;
        }
    }
}