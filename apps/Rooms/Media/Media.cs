using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using JetBrains.Annotations;
using NetDaemon.Common.Reactive;

[UsedImplicitly]
public class Media : RoomApp
{
    protected override bool IndoorRoom => true;
    protected override TimeSpan OccupancyTimeout => TimeSpan.FromMinutes(10);

    public override void Initialize()
    {
        // Lights off when movie is playing
        Entity("media_player.media_emby")
            .StateChangesFiltered()
            .Where(s =>
                s.New!.State == "playing"
                && s.New.Attribute!.media_content_type == "movie")
            .NDSameStateFor(TimeSpan.FromMinutes(3))
            .Subscribe(_ => Entities(e => e.EntityId.StartsWith("light.")).TurnOff());

        // Lights on when 5 minutes before end of movie
        Entity("media_player.media_emby")
            .StateAllChanges
            .Where(s =>
                s.New!.Attribute!.media_content_type == "movie" &&
                s.New.Attribute!.media_duration - s.New.Attribute.media_position == 300 &&
                s.New.State == "playing")
            .Subscribe(_ => Entity("light.media").TurnOn());

        // Light toilet when paused
        Entity("media_player.media_emby")
            .StateAllChanges
            .Where(s =>
                s.New!.Attribute!.media_content_type == "movie" &&
                s.Old!.State == "playing" &&
                s.New.State == "paused")
            .Subscribe(_ =>
                Entities(new List<string> {"light.media", "light.dining", "light.hallway", "light.toilet"}).TurnOn());

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