using System;
using System.Reactive.Linq;
using JetBrains.Annotations;
using NetDaemon.Common.Reactive;

[UsedImplicitly]
public class Study : RoomApp
{
    protected override bool IndoorRoom => true;
    protected override TimeSpan OccupancyTimeout => TimeSpan.FromMinutes(10);

    public string? MonitorSwitch => "switch.office_pc_monitors";
    public string? PcUsage => "binary_sensor.studypc_on";

    public override void Initialize()
    {
        Entity(PcUsage!)
            .StateChangesFiltered()
            .Where(s => s.Old.State == "off" && s.New.State == "on")
            .NDSameStateFor(TimeSpan.FromSeconds(3))
            .Subscribe(_ => PcInUseAction());
            

        Entity(PcUsage!)
            .StateChangesFiltered()
            .Where(s => s.Old.State == "on" && s.New.State == "off")
            .NDSameStateFor(TimeSpan.FromMinutes(1))
            .Subscribe(_ => PcNotInUseAction());

        base.Initialize();
    }

    private void PcInUseAction()
    {
        Entity(MonitorSwitch!).TurnOn();
    }

    private void PcNotInUseAction()
    {
        Entity(MonitorSwitch!).TurnOff();
    }
}