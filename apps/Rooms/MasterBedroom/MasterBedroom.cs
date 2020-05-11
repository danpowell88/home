using System;
using System.Linq;
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

        base.InitializeAsync();

        return Task.CompletedTask;
    }

    private async Task TurnEveryThingOff(string arg1, EntityState? arg2, EntityState? arg3)
    {
        await this.TurnEverythingOff(excludeEntities: "fan.masterbedroom_fan");
    }

    protected override bool IndoorRoom => true;
    protected override TimeSpan OccupancyTimeout => TimeSpan.FromMinutes(60);

    protected override bool PresenceLightingEnabled
    {
        get
        {
            var bedState = State.Single(e => e.EntityId == "binary_sensor.bed_occupancy").State;

            return base.PresenceLightingEnabled && bedState != null && bedState != "on";
        }
    }

    //protected override bool DebugMode => true;
}