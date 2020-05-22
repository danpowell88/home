using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using JetBrains.Annotations;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

[UsedImplicitly]
public class Media : RoomApp
{
    protected override bool IndoorRoom => true;
    protected override TimeSpan OccupancyTimeout => TimeSpan.FromMinutes(10);

    public override Task InitializeAsync()
    {
        // Lights off when movie is playing
        Entity("media_player.media_emby")
            .WhenStateChange((to, from) => 
                to!.State == "playing"
                    && to.Attribute!.media_content_type == "movie")
            .AndNotChangeFor(TimeSpan.FromMinutes(3))
            .UseEntities(e => e.EntityId.StartsWith("light."))
            .TurnOff()
            .Execute();

        // Lights on when 5 minutes before end of movie
        Entity("media_player.media_emby")
            .WhenStateChange((to, from) =>
                to!.Attribute!.media_content_type == "movie" &&
                to.Attribute.media_duration - to.Attribute.media_position == 300 &&
                to.State == "playing")
            .UseEntities(e => e.EntityId.StartsWith("light.media"))
            .TurnOn()
            .Execute();

        // Light toilet when paused
        Entity("media_player.media_emby")
            .WhenStateChange((to, from) =>
                to!.Attribute!.media_content_type == "movie" &&
                from!.State == "playing" &&
                to.State == "paused")
            .UseEntities(new List<string> {"light.media", "light.dining", "light.hallway", "light.toilet"})
            .TurnOn()
            .Execute();

        return base.InitializeAsync();
    }

    protected override bool PresenceLightingEnabled
    {
        get
        {
            var state = GetState("media_player.media_emby")!.State;

            return state != "playing" && base.PresenceLightingEnabled;
        }
    }
}