using System;

public class EnsuiteShower : RoomApp
{
    protected override bool IndoorRoom => true;

    protected override string RoomPrefix => "ensuiteshower";
    protected override TimeSpan OccupancyTimeout => TimeSpan.FromMinutes(10);
}