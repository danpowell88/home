using System;

public class MasterBedroomRobe : RoomApp
{
    protected override bool IndoorRoom => true;

    protected override TimeSpan OccupancyTimeout => TimeSpan.FromMinutes(3);
}