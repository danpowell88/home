using System;

public class Pantry : RoomApp
{
    protected override bool IndoorRoom => true;

    protected override string RoomPrefix => "pantry";
    protected override TimeSpan OccupancyTimeout => TimeSpan.FromMinutes(3);
}