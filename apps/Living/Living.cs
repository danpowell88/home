using System;

public class Living : RoomApp
{
    protected override bool IndoorRoom => true;

    protected override string RoomPrefix => "living";
    protected override TimeSpan OccupancyTimeout => TimeSpan.FromMinutes(30);
}