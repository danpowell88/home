using System;
using JetBrains.Annotations;

[UsedImplicitly]
public class Living : RoomApp
{
    protected override bool IndoorRoom => true;
    protected override TimeSpan OccupancyTimeout => TimeSpan.FromMinutes(30);
}