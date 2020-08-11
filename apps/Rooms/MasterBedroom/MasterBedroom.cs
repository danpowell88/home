using System;
using System.Collections.Generic;
using JetBrains.Annotations;

[UsedImplicitly]
public class MasterBedroom : RoomApp
{
    protected override void TurnEveryThingOff()
    {
        if (!this.IsAnyoneInBed())
        {
            this.TurnEverythingOff(excludeEntities: new string[0]);
        }
        else if (this.IsEveryoneInBed())
        {
            this.TurnEverythingOff(excludeEntities: "fan.masterbedroom_fan");
        }
        else
        {
             this.TurnEverythingOff(RoomName, excludeEntities: "fan.masterbedroom_fan");
             this.TurnEverythingOff(nameof(MasterBedroomRobe));
             this.TurnEverythingOff(nameof(Ensuite));
             this.TurnEverythingOff(nameof(EnsuiteShower));
             this.TurnEverythingOff(nameof(Entry));
        }
    }

    protected override bool SecondaryLightingEnabled => State("person.marissa")!.State == "not_home" ||
                                                        State("input_boolean.party_mode")!.State == "on";

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
            var bed = State("binary_sensor.bed_occupancy")!;
            var bedState = bed.State;

            // only control lighting when no one in bed and has been that way for 10 mins
            return base.PresenceLightingEnabled && bedState != null && bedState != "on";
            //&& DateTime.Now - bed.LastChanged > TimeSpan.FromMinutes(10);
        }
    }
}