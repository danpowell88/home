using System;
using JetBrains.Annotations;

[UsedImplicitly]
public class Entry : RoomApp
{
    protected override bool IndoorRoom => true;

    protected override TimeSpan OccupancyTimeout => TimeSpan.FromMinutes(2);

    protected override bool PresenceLightingEnabled => !this.IsAnyoneInBed() && base.PresenceLightingEnabled;
}