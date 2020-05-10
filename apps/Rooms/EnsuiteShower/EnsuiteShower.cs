using System;
using System.Threading.Tasks;
using JetBrains.Annotations;

[UsedImplicitly]
public class EnsuiteShower : RoomApp
{
    protected override bool IndoorRoom => true;
    protected override TimeSpan OccupancyTimeout => TimeSpan.FromMinutes(10);

    public override Task InitializeAsync()
    {
        Entity("fan.ensuiteshower_fan")
            .WhenStateChange(from: "off", to: "on")
            .AndNotChangeFor(OccupancyTimeoutObserved)
            .UseEntity("fan.ensuiteshower_fan")
            .TurnOff()
            .Execute();

        return base.InitializeAsync();
    }
}