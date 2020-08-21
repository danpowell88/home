using System;
using System.Reactive.Linq;
using JetBrains.Annotations;
using NetDaemon.Common.Reactive;

[UsedImplicitly]
public class Bathroom : RoomApp
{
    protected override bool IndoorRoom => true;
    protected override TimeSpan OccupancyTimeout => TimeSpan.FromMinutes(2);

    public override void Initialize()
    {
        Entity("fan.bathroom_fan")
            .StateChangesFiltered()
            .Where(tuple => tuple.Old.State == "off" &&  tuple.New.State == "on")
            .NDSameStateFor(OccupancyTimeoutObserved)
            .Subscribe(tuple =>
            {
                LogHistory($"Turn off bathroom fan after {OccupancyTimeoutObserved.TotalMinutes} minutes");
                Entity(tuple.New.EntityId).TurnOff();
            });
            

        base.Initialize();
    }
}