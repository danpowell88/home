using System;

public class Dining : RoomApp
{
    protected override bool IndoorRoom => true;

    protected override TimeSpan OccupancyTimeout => TimeSpan.FromMinutes(10);
}