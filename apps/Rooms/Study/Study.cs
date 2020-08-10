using System;
using JetBrains.Annotations;

[UsedImplicitly]
public class Study : RoomApp
{
    protected override bool IndoorRoom => true;
    //protected override bool DebugMode => true;
    protected override TimeSpan OccupancyTimeout => TimeSpan.FromMinutes(10);

    public string? MonitorSwitch => "switch.office_pc_monitors";
    public string? PcUsage => "binary_sensor.studypc_on";

    public override void Initialize()
    {
        
            //Entity(PcUsage!)
            //    .WhenStateChange(from: "off", to: "on")
            //    .AndNotChangeFor(TimeSpan.FromSeconds(3))
            //    .Call(PcInUseAction)
            //    .Execute();

            //Entity(PcUsage!)
            //    .WhenStateChange(from: "on", to: "off")
            //    .AndNotChangeFor(TimeSpan.FromMinutes(1))
            //    .Call(PcNotInUseAction)
            //    .Execute();

            base.Initialize();
    }

    //private async Task PcInUseAction(string arg1, EntityState? arg2, EntityState? arg3)
    //{
    //    await Entity(MonitorSwitch!).TurnOn().ExecuteAsync();
    //}

    //private async Task PcNotInUseAction(string arg1, EntityState? arg2, EntityState? arg3)
    //{
    //    await Entity(MonitorSwitch!).TurnOff().ExecuteAsync();
    //}
}