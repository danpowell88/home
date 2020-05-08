using System;
using System.Threading.Tasks;
using JetBrains.Annotations;

[UsedImplicitly]
public class EnsuiteToilet : RoomApp
{
    protected override bool IndoorRoom => true;
    protected override bool PresenceLightingEnabled => false;
    protected override TimeSpan OccupancyTimeout => TimeSpan.FromMinutes(10);

    public override Task InitializeAsync()
    {
        Entity("light.ensuitetoilet")
            .WhenStateChange(from: "off", to: "on")
            .AndNotChangeFor(OccupancyTimeoutObserved).UseEntity("light.ensuitetoilet").TurnOff().Execute();

        return base.InitializeAsync();
    }
}