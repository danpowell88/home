using System;

public class Pantry : RoomApp
{
    protected override bool IndoorRoom => true;

    protected override TimeSpan OccupancyTimeout => TimeSpan.FromMinutes(3);
}