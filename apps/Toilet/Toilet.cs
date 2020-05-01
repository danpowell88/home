using System;

public class Toilet : RoomApp
{
    protected override bool IndoorRoom => true;

    protected override string RoomPrefix => "Toilet";
    protected override TimeSpan OccupancyTimeout => TimeSpan.FromMinutes(5);
}