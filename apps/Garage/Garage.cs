using System;
using System.Threading.Tasks;
using JoySoftware.HomeAssistant.NetDaemon.Common;

public class GarageApp : RoomApp
{
    protected override bool IndoorRoom => true;

    protected override string RoomPrefix => "garage";
    protected override TimeSpan OccupancyTimeout => TimeSpan.FromMinutes(10);

}