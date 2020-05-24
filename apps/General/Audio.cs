using System;
using System.Threading.Tasks;
using JetBrains.Annotations;
using JoySoftware.HomeAssistant.NetDaemon.Common;

[UsedImplicitly]
public class Audio : NetDaemonApp
{
    public override async Task InitializeAsync()
    {
        Entity("group.family").WhenStateChange(from: "not_home", to: "home").Call(async (_, __, ___) => await this.SetTTSVolume()).Execute();
        Entity("binary_sensor.bed_occupancy").WhenStateChange().Call(async (_, __, ___) => await this.SetTTSVolume()).Execute();
        await base.InitializeAsync();
    }
}