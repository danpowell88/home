using System;
using JetBrains.Annotations;

[UsedImplicitly]
public class Dining : RoomApp
{
    protected override bool IndoorRoom => true;
    protected override TimeSpan OccupancyTimeout => TimeSpan.FromMinutes(10);

    protected override bool SecondaryLightingEnabled => DateTime.Now.Hour >= 18 && DateTime.Now.Hour <= 22;
}