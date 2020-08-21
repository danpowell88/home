using System;
using System.Reactive.Linq;
using JetBrains.Annotations;
using NetDaemon.Common.Reactive;

[UsedImplicitly]
public class Garage : RoomApp
{
    protected override bool IndoorRoom => true;
    protected override TimeSpan OccupancyTimeout => TimeSpan.FromMinutes(10);

    private IDisposable? _garageOpenTimer;

    public override void Initialize()
    {
        Entity("cover.garage_door")
            .StateChangesFiltered()
            .Where(s => s.Old.State == "closed" && s.New.State == "open")
            .Subscribe(_ => Entities(Lights).TurnOn());

        Entity("cover.garage_door")
            .StateChangesFiltered()
            .Where(s => s.Old.State == "closed" && s.New.State == "open")
            .NDSameStateFor(TimeSpan.FromMinutes(20))
            .Subscribe(s =>
            {
                LogHistory($"Garage door left open");

                CancelOpenTimer();

                _garageOpenTimer = RunEvery(TimeSpan.FromMinutes(20), () =>
                {
                    if (State("cover.garage_door")!.State == "open")
                    {
                        var filename = $"{Guid.NewGuid()}.jpg";

                        CallService("camera", "snapshot", new
                        {
                            filename = $"www/snapshots/tmp/{filename}",
                            entity_id = "camera.garage"
                        });

                        this.Notify(
                            "Security",
                            $"The garage door has been left open",
                            Notifier.NotificationCriteria.Always,
                            Notifier.NotificationCriteria.Home,
                            new[]
                            {
                                new Notifier.NotificationAction("close_garage", "Close"),
                                new Notifier.NotificationAction("silence_garage", "Silence")
                            },
                            $"https://home.danielpowell.net/local/snapshots/tmp/{filename}",
                            Notifier.TextNotificationDevice.All);
                    }
                });
            });

        EventChanges
            .Where(e => e.Event == "mobile_app_notification_action" && e.Data!.action == "close_garage")
            .Subscribe(_ =>
            {
                LogHistory($"Mobile application close garage door");

                if (State("cover.garage_door")!.State == "open")
                {
                    CallService("cover", "close_cover", new {entity_id = "cover.garage_door"});
                }
            });

        EventChanges
            .Where(e => e.Event == "mobile_app_notification_action" && e.Data!.action == "silence_garage")
            .Subscribe(_ =>
            {
                LogHistory($"Mobile application silence garage alert");
                CancelOpenTimer();
            });

        Entity("cover.garage_door")
            .StateChangesFiltered()
            .Where(s => s.Old.State=="open" && s.New.State == "closed")
            .Subscribe(_ =>
            {
                LogHistory($"Garage door closed, cancel timer");
                CancelOpenTimer();
            });

        base.Initialize();
    }

    private void CancelOpenTimer()
    {
        if (_garageOpenTimer != null)
        {
            _garageOpenTimer.Dispose();
            _garageOpenTimer = null;
        }
    }
}