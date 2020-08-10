using System;
using System.Collections.Generic;
using JetBrains.Annotations;

[UsedImplicitly]
public class Media : RoomApp
{
    protected override bool IndoorRoom => true;
    protected override TimeSpan OccupancyTimeout => TimeSpan.FromMinutes(10);

    public override void Initialize()
    {
        // TODO: lambda causes this to fire continuosly after 3 minutes so lights always keep turning off
        // Lights off when movie is playing
        //Entity("media_player.media_emby")
        //    .WhenStateChange((to, from) => 
        //        to!.State == "playing"
        //            && to.Attribute!.media_content_type == "movie")
        //    .AndNotChangeFor(TimeSpan.FromMinutes(3))
        //    .UseEntities(e => e.EntityId.StartsWith("light."))
        //    .TurnOff()
        //    .Execute();

        // TODO: this didnt seem to trigger and would cause lights to continuosly turn on, maybe not that much of a big deal but ineffecient
        // Lights on when 5 minutes before end of movie
        //Entity("media_player.media_emby")
        //    .WhenStateChange((to, from) =>
        //        to!.Attribute!.media_content_type == "movie" &&
        //        to.Attribute.media_duration - to.Attribute.media_position == 300 &&
        //        to.State == "playing")
        //    .UseEntities(e => e.EntityId.StartsWith("light.media"))
        //    .TurnOn()
        //    .Execute();

        //// Light toilet when paused
        //Entity("media_player.media_emby")
        //    .WhenStateChange((to, from) =>
        //        to!.Attribute!.media_content_type == "movie" &&
        //        from!.State == "playing" &&
        //        to.State == "paused")
        //    .UseEntities(new List<string> {"light.media", "light.dining", "light.hallway", "light.toilet"})
        //    .TurnOn()
        //    .Execute();

        base.Initialize();
    }

    protected override bool PresenceLightingEnabled
    {
        get
        {
            var state = State("media_player.media_emby")!.State;

            return state != "playing" && base.PresenceLightingEnabled;
        }
    }
}