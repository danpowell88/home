using System;
using System.Threading.Tasks;
using JetBrains.Annotations;
using JoySoftware.HomeAssistant.NetDaemon.Common;

[UsedImplicitly]
public class Audio : NetDaemonApp
{
    public override async Task InitializeAsync()
    {
        Scheduler.RunEvery(TimeSpan.FromMinutes(15), async () => await this.SetTTSVolume());

        Entity("sensor.bed_occupancy_count").WhenStateChange().Call(async (_, __, ___) => await this.SetTTSVolume()).Execute();

        await base.InitializeAsync();
    }
}