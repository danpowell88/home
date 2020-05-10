using System;
using System.Threading.Tasks;
using JetBrains.Annotations;

[UsedImplicitly]
public class Bathroom : RoomApp
{
    protected override bool IndoorRoom => true;
    protected override TimeSpan OccupancyTimeout => TimeSpan.FromMinutes(2);

    public override Task InitializeAsync()
    {
        Entity("fan.bathroom_fan")
            .WhenStateChange(from: "off", to: "on")
            .AndNotChangeFor(OccupancyTimeoutObserved)
            .UseEntity("fan.bathroom_fan")
            .TurnOff()
            .Execute();

        return base.InitializeAsync();
    }
}