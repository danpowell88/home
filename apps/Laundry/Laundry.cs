using System;
using System.Threading.Tasks;
using JoySoftware.HomeAssistant.NetDaemon.Common;

public class LaundryApp : RoomApp
{
    protected override bool IndoorRoom => true;

    protected override TimeSpan OccupancyTimeout => TimeSpan.FromMinutes(5);

}