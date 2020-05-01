using System;

public class Ensuite : RoomApp
{
    protected override bool IndoorRoom => true;

    protected override bool DebugLogEnabled => true;
    protected override string RoomPrefix => "ensuite";
    protected override TimeSpan OccupancyTimeout => TimeSpan.FromMinutes(10);
}