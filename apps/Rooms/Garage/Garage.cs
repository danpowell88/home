using System;
using System.Threading.Tasks;
using JetBrains.Annotations;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

[UsedImplicitly]
public class Garage : RoomApp
{
    protected override bool IndoorRoom => true;
    protected override TimeSpan OccupancyTimeout => TimeSpan.FromMinutes(10);

    private const string IgnoreOpenKey = "IgnoreOpen";

    public override Task InitializeAsync()
    {
        Scheduler.RunEvery(TimeSpan.FromMinutes(20), async () =>
        {
            var garage = GetState("cover.garage_door");
            
            if (garage!.State == "open" && DateTime.Now - garage.LastChanged > TimeSpan.FromMinutes(20) && Storage.SilenceGarageOpen ?? false)
            {
                var filename = $"{Guid.NewGuid()}.jpg";

                await CallService("camera", "snapshot", new
                {
                    filename = $"www/snapshots/tmp/{filename}",
                    entity_id = "camera.garage"
                }, true);

                await this.Notify(
                    "Security",
                    "The garage door has been left open",
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

        Events(e => e.EventId == "mobile_app_notification_action" && e.Data!.action == "close_garage")
            .Call(async (_, __) =>
            {
                if (GetState("cover.garage_door")!.State == "open")
                {
                    await CallService("cover", "close_cover", new {entity_id = "cover.garage_door"});
                }
            });

        Events(e => e.EventId == "mobile_app_notification_action" && e.Data!.action == "silence_garage")
            .Call(async (_, __) =>
            {
                Storage.SilenceGarageOpen = true;
                await Task.CompletedTask;
            });

        Entity("cover.garage_door")
            .WhenStateChange(from: "open", to: "closed").Call((_, __, ___) => Storage.SilenceGarageOpen = false);

        return base.InitializeAsync();
    }
}