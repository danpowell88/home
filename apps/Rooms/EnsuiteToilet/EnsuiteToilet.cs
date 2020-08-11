using System;
using System.Reactive.Linq;
using JetBrains.Annotations;
using NetDaemon.Common.Reactive;

[UsedImplicitly]
public class EnsuiteToilet : RoomApp
{
    protected override bool IndoorRoom => true;
    protected override bool PresenceLightingEnabled => false;
    protected override TimeSpan OccupancyTimeout => TimeSpan.FromMinutes(10);

    public override void Initialize()
    {
        Entity("light.ensuitetoilet")
            .StateChangesFiltered()
            .Where(tuple => tuple.Old.State == "off" && tuple.New.State == "on")
            .NDSameStateFor(OccupancyTimeoutObserved)
            .Subscribe(tuple => Entity(tuple.New.EntityId).TurnOff());

        base.Initialize();
    }
}