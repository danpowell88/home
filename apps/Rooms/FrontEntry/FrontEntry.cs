using System;
using JetBrains.Annotations;

[UsedImplicitly]
public class FrontEntry : RoomApp
{
    protected override bool IndoorRoom => false;

    protected override bool DebugMode => true;
    protected override TimeSpan OccupancyTimeout => TimeSpan.FromMinutes(3);

    public override void Initialize()
    {
        //Entity("binary_sensor.doorbell_ringing")
        //    .WhenStateChange(from: "off", to: "on")
        //    .Call(DoorbellAction)
        //    .Execute();

        //Entity("sun.sun")
        //    .WhenStateChange((to, from) =>
        //        to!.Attribute!.elevation <= 2.5 &&
        //        to!.Attribute!.rising == false &&
        //        DateTime.Now.Hour < 21 &&
        //        !this.IsAnyoneSleeping()
        //        )
        //    .UseEntity("light.front_pillars")
        //    .TurnOn()
        //    .Execute();

        //Scheduler.RunDaily("21:00:00", async () => 
        //    await 
        //        Entity("light.front_pillars")
        //        .TurnOff()
        //        .ExecuteAsync());

        base.Initialize();
    }

    //private async Task DoorbellAction(string arg1, EntityState? arg2, EntityState? arg3)
    //{
    //    this.Notify(
    //        "Security", 
    //        "The doorbell has been rung", 
    //        Notifier.NotificationCriteria.Always,
    //        Notifier.NotificationCriteria.None,
    //        Notifier.TextNotificationDevice.All);

    //    this.Notify(new Uri("http://192.168.1.2:8123/local/doorbell.mp3"),1, Notifier.AudioNotificationDevice.Home);
    //}
}