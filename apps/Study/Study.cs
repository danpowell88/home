using System;
using System.Threading.Tasks;
using JoySoftware.HomeAssistant.NetDaemon.Common;

public class StudyApp : RoomApp
{
    public string? MonitorSwitch { get; set; }
    public string? PcUsage { get; set; }

    public override Task InitializeAsync()
    {
        Entity(PcUsage!)
            .WhenStateChange(from: "off", to: "on")
            .AndNotChangeFor(TimeSpan.FromSeconds(10))
            .Call(PcInUseAction)
            .Execute();

        Entity(PcUsage!)
            .WhenStateChange(from: "on", to: "off")
            .AndNotChangeFor(TimeSpan.FromMinutes(2))
            .Call(PcNotInUseAction)
            .Execute();

        return base.InitializeAsync();
    }

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