using System;
using System.Reactive.Linq;
using JetBrains.Annotations;

[UsedImplicitly]
public class FrontEntry : RoomApp
{
    protected override bool IndoorRoom => false;

    protected override bool DebugMode => false;
    protected override TimeSpan OccupancyTimeout => TimeSpan.FromMinutes(3);

    public override void Initialize()
    {
        Entity("binary_sensor.doorbell_ringing")
            .StateChangesFiltered()
            .Where(e => e.Old.State == "off" && e.New.State == "on")
            .Subscribe(e =>
            {
                this.Notify(
                    "Security",
                    "The doorbell has been rung",
                    Notifier.NotificationCriteria.Always,
                    Notifier.NotificationCriteria.None,
                    Notifier.TextNotificationDevice.All);

                this.Notify(new Uri("http://192.168.1.2:8123/local/doorbell.mp3"), 1,
                    Notifier.AudioNotificationDevice.Home);
            });

        Entity("sun.sun")
            .StateChangesFiltered()
            .Where(s => s.New.Attribute!.elevation <= 2.5 &&
                        s.New!.Attribute!.rising == false &&
                        DateTime.Now.Hour < 21 &&
                        !this.IsAnyoneSleeping())
            .Subscribe(s => Entity("light.front_pillars").TurnOn());


        RunDaily("21:00:00", () => Entity("light.front_pillars").TurnOff());

        base.Initialize();
    }
}