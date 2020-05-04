using System;
using JetBrains.Annotations;

[UsedImplicitly]
public class GarageOutside : RoomApp
{
    protected override bool IndoorRoom => false;
    protected override TimeSpan OccupancyTimeout => TimeSpan.FromMinutes(2);
}