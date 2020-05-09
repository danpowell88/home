using System;
using System.Threading.Tasks;
using JetBrains.Annotations;
using JoySoftware.HomeAssistant.NetDaemon.Common;

[UsedImplicitly]
public class MasterBedroom : RoomApp
{
    public override Task InitializeAsync()
    {
        Entity("sensor.daniel_bedroom_switch_click", "sensor.riss_bedroom_switch_click")
            .WhenStateChange(to: "single")
            .Call(TurnEveryThingOff)
            .Execute();


        return Task.CompletedTask;
    }

    private async Task TurnEveryThingOff(string arg1, EntityState? arg2, EntityState? arg3)
    {
        await this.TurnEverythingOff(excludeEntities:"fan.masterbedroom_fan");
    }
    protected override bool IndoorRoom => true;
    protected override TimeSpan OccupancyTimeout => TimeSpan.FromHours(2);
    protected override bool DebugMode => false;
    protected override bool PresenceLightingEnabled => false;
}