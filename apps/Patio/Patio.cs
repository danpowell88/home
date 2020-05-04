using System;
using JetBrains.Annotations;

[UsedImplicitly]
public class Patio : RoomApp
{
    protected override bool IndoorRoom => false;
    protected override TimeSpan OccupancyTimeout => TimeSpan.FromMinutes(10);
}