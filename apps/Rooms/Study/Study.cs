using System;
using System.Reactive.Linq;
using JetBrains.Annotations;
using NetDaemon.Common.Reactive;

[UsedImplicitly]
public class Study : RoomApp
{
    protected override bool IndoorRoom => true;
    protected override TimeSpan OccupancyTimeout => TimeSpan.FromMinutes(10);

    public string MonitorSwitch => "switch.office_pc_monitors";
    public string PcUsage => "sensor.study_pc_wattage";

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

        Entities(PowerSensors)
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