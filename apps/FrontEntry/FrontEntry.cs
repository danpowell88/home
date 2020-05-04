using System;
using JetBrains.Annotations;

[UsedImplicitly]
public class FrontEntry : RoomApp
{
    protected override bool IndoorRoom => false;
    protected override TimeSpan OccupancyTimeout => TimeSpan.FromMinutes(3);
}