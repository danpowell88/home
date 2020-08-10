using System;
using JetBrains.Annotations;

[UsedImplicitly]
public class EnsuiteShower : RoomApp
{
    protected override bool IndoorRoom => true;
    protected override TimeSpan OccupancyTimeout => TimeSpan.FromMinutes(10);

    public override void Initialize()
    {
        //Entity("fan.ensuiteshower_fan")
        //    .WhenStateChange(from: "off", to: "on")
        //    .AndNotChangeFor(OccupancyTimeoutObserved)
        //    .UseEntity("fan.ensuiteshower_fan")
        //    .TurnOff()
        //    .Execute();

        base.Initialize();
    }
}