using System;
using System.Threading.Tasks;
using JetBrains.Annotations;
using JoySoftware.HomeAssistant.NetDaemon.Common;

[UsedImplicitly]
public class Study : RoomApp
{
    public string? MonitorSwitch { get; set; }
    public string? PcUsage { get; set; }

    public override Task InitializeAsync()
    {
        Entity(PcUsage!)
            .WhenStateChange(from: "off", to: "on")
            .AndNotChangeFor(TimeSpan.FromSeconds(3))
            .Call(PcInUseAction)
            .Execute();

        Entity(PcUsage!)
            .WhenStateChange(from: "on", to: "off")
            .AndNotChangeFor(TimeSpan.FromMinutes(2))
            .Call(PcNotInUseAction)
            .Execute();

        return base.InitializeAsync();
    }

    protected override bool DebugMode => true;

    private async Task PcInUseAction(string arg1, EntityState? arg2, EntityState? arg3)
    {
        await Entity(MonitorSwitch!).TurnOn().ExecuteAsync();
    }

    private async Task PcNotInUseAction(string arg1, EntityState? arg2, EntityState? arg3)
    {
       await Entity(MonitorSwitch!).TurnOff().ExecuteAsync();
    }

    protected override bool IndoorRoom => true;
    protected override TimeSpan OccupancyTimeout => TimeSpan.FromMinutes(10);
}