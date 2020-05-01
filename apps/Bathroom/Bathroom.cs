using System;

public class Bathroom : RoomApp
{
    protected override bool IndoorRoom => true;

    protected override string RoomPrefix => "bathroom";
    protected override TimeSpan OccupancyTimeout => TimeSpan.FromMinutes(2);
}