using System;

public class Toilet : RoomApp
{
    protected override bool IndoorRoom => true;
    protected override bool DebugLogEnabled => true;
    protected override TimeSpan OccupancyTimeout => TimeSpan.FromMinutes(5);
}