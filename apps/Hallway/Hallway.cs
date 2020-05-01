using System;

public class Hallway : RoomApp
{
    protected override bool IndoorRoom => true;

    protected override string RoomPrefix => "hallway";
    protected override TimeSpan OccupancyTimeout => TimeSpan.FromMinutes(2);
}