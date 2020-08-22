using System;
using System.Reactive.Linq;
using daemonapp.Utilities;
using JetBrains.Annotations;
using NetDaemon.Common.Reactive;

[UsedImplicitly]
public class Study : RoomApp
{
    protected override bool IndoorRoom => true;
    protected override TimeSpan OccupancyTimeout => TimeSpan.FromMinutes(10);

    public string MonitorSwitch => "switch.pc_monitors_plug";
    public string PcUsage => "sensor.office_pc_power";

    public override void Initialize()
    {
        Entity(PcUsage)
                .StateChangesFiltered()
                .Where(s =>
                    s.Old.State < s.Old.Attribute?.active_threshold &&
                    s.New.State >= s.New.Attribute?.active_threshold)
                .NDSameStateFor(TimeSpan.FromSeconds(3))
                .Subscribe(_ =>
                {
                    LogHistory("PC above power threshold");
                    PcInUseAction();
                });

        Entities(EntityLocator.PowerSensors(RoomName))
                .StateChangesFiltered()
                .Where(s =>
                    s.Old.State >= s.Old.Attribute?.active_threshold &&
                    s.New.State < s.New.Attribute?.active_threshold)
                .NDSameStateFor(TimeSpan.FromMinutes(1))
                .Subscribe(_=>
                {
                    LogHistory("PC below power threshold");
                    PcNotInUseAction();
                });

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