using System;
using JetBrains.Annotations;

[UsedImplicitly]
public class GymOutside : RoomApp
{
    protected override bool IndoorRoom => false;
    protected override bool DebugMode => true;
    protected override TimeSpan OccupancyTimeout => TimeSpan.FromMinutes(2);
}