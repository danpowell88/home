using System;
using System.Threading.Tasks;
using JetBrains.Annotations;
using JoySoftware.HomeAssistant.NetDaemon.Common;

[UsedImplicitly]
public class FrontEntry : RoomApp
{
    protected override bool IndoorRoom => false;
    protected override TimeSpan OccupancyTimeout => TimeSpan.FromMinutes(3);

    public override Task InitializeAsync()
    {
        Entity("binary_sensor.doorbell_ringing")
            .WhenStateChange(from: "off", to: "on")
            .Call(DoorbellAction)
            .Execute();

        Entity("sun.sun")
            .WhenStateChange((to, from) =>
                from!.Attribute!.elevation > 4L &&
                to!.Attribute!.elevation <= 4L &&
                to!.Attribute!.rising == false
            ).UseEntity("light.front_pillars").TurnOn().Execute();

        Scheduler.RunDaily("21:00:00", async () => await Entity("light.front_pillars").TurnOff().ExecuteAsync());

        return base.InitializeAsync();
    }

    private async Task DoorbellAction(string arg1, EntityState? arg2, EntityState? arg3)
    {
        await CallService("notify", "mobile_app_daniel_s10", new
        {
            message = "The doorbell has been rung",
            title = "Security"
        });

        // todo: get volume before, raise volume, set volume back to previous

        await CallService("media_player", "play_media", new
        {
            entity_id = "media_player.home_2",
            media_content_id = "http://192.168.1.2:8123/local/doorbell.mp3",
            media_content_type = "music"
        });
    }
}