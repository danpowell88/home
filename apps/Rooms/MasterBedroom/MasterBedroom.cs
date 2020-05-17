using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;
using JoySoftware.HomeAssistant.NetDaemon.Common;

[UsedImplicitly]
public class MasterBedroom : RoomApp
{
    public override Task InitializeAsync()
    {
        Entity("binary_sensor.bed_occupancy")
            .WhenStateChange(from: "on", to: "off")
            .Call((_, __, ___) => StartTimer());



        return base.InitializeAsync();
    }

    protected override async Task TurnEveryThingOff()
    {
        await this.TurnEverythingOff(excludeEntities: "fan.masterbedroom_fan");
    }

    protected override bool SecondaryLightingEnabled => (DateTime.Now.Hour >= 18 && DateTime.Now.Hour <= 24) ||
                                                        (DateTime.Now.Hour >= 0 && DateTime.Now.Hour <= 5) ||
                                                        GetState("input_boolean.party_mode")!.State == "on";

    protected override Dictionary<string, object>? SecondaryLightingAttributes
    {
        get
        {
            var rand = new Random();

            var colours = new List<int> {rand.Next(0, 255), rand.Next(0, 255), rand.Next(0, 255)};

            return new Dictionary<string, object>
            {
                {
                    "rgb_color",  colours
                }
            };
        }
    }

    protected override bool IndoorRoom => true;
    protected override TimeSpan OccupancyTimeout => TimeSpan.FromMinutes(15);

    protected override bool PresenceLightingEnabled
    {
        get
        {
            var bed = State.Single(e => e.EntityId == "binary_sensor.bed_occupancy");
            var bedState = bed.State;

            // only control lighting when no one in bed and has been that way for 10 mins
            return base.PresenceLightingEnabled && bedState != null && bedState != "on" &&
                   DateTime.Now - bed.LastChanged > TimeSpan.FromMinutes(10);
        }
    }
}