using System;
using JetBrains.Annotations;

[UsedImplicitly]
public class EnsuiteToilet : RoomApp
{
    protected override bool IndoorRoom => true;
    protected override bool PresenceLightingEnabled => false;
    protected override TimeSpan OccupancyTimeout => TimeSpan.FromMinutes(10);

    public override void Initialize()
    {
        //Entity("light.ensuitetoilet")
        //    .WhenStateChange(from: "off", to: "on")
        //    .AndNotChangeFor(OccupancyTimeoutObserved)
        //    .UseEntity("light.ensuitetoilet")
        //    .TurnOff()
        //    .Execute();

        base.Initialize();
    }
}