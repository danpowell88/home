using System;
using System.Reactive.Linq;
using JetBrains.Annotations;
using NetDaemon.Common.Reactive;

[UsedImplicitly]
public class EnsuiteShower : RoomApp
{
    protected override bool IndoorRoom => true;
    protected override TimeSpan OccupancyTimeout => TimeSpan.FromMinutes(10);

    public override void Initialize()
    {
        Entity("fan.ensuiteshower_fan")
            .StateChangesFiltered()
            .Where(tuple => tuple.Old.State == "off" && tuple.New.State == "on")
            .NDSameStateFor(OccupancyTimeoutObserved)
            .Subscribe(tuple =>
            {
                LogHistory($"Turn off ensuite shower fan after {OccupancyTimeoutObserved.TotalMinutes} minutes");
                Entity(tuple.New.EntityId).TurnOff();
            });

        base.Initialize();
    }
}