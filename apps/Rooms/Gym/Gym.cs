using System;
using System.Reactive.Linq;
using JetBrains.Annotations;
using NetDaemon.Common.Reactive;

[UsedImplicitly]
public class Gym : RoomApp
{
    private const string Training = "sensor.gym_trainer_wattage";
    private const string Climate = "sensor.gym_temperature";
    private const string BikeFanSwitch = "switch.gym_bike_fan";
    private const string WeightFanSwitch = "switch.gym_weights_fan";
    private const string FanButton = "sensor.gym_switch_click";
    private const int FanTriggerTemp = 25;

    public override void Initialize()
    {
        Entity(Training!)
            .StateChangesFiltered()
            .Where(s =>
                (s.New!.State ?? 0L) >= s.New.Attribute!.active_threshold &&
                State(Climate!)!.State >= FanTriggerTemp)
            .NDSameStateFor(TimeSpan.FromMinutes(1))
            .Subscribe(_ => BikeTrainingAction());


        Entity(Climate!)
            .StateChangesFiltered()
            .Where(s =>
                (decimal?)s.Old!.State! < FanTriggerTemp &&
                (decimal?)s.New!.State! >= FanTriggerTemp &&
                State(Training!)!.State == "on")
            .Subscribe(_ => BikeTrainingAction());


        Entity(Training!)
            .StateChangesFiltered()
            .Where(s =>
                (s.New!.State ?? 0L) < s.New.Attribute!.active_threshold)
            .NDSameStateFor(TimeSpan.FromMinutes(2))
            .Subscribe(_ => NoBikeTrainingAction());


        Entity(FanButton!)
            .StateChangesFiltered()
            .Where(s => s.New.State == "left")
            .Subscribe(_ => ToggleBikeFan());

        Entity(FanButton!)
          .StateChangesFiltered()
            .Where(s => s.New.State == "right")
            .Subscribe(_ => ToggleWeightFan());

        base.Initialize();
    }

    private void ToggleWeightFan()
    {
        Entity(WeightFanSwitch!).Toggle();
    }

    private void ToggleBikeFan()
    {
        Entity(BikeFanSwitch!).Toggle();
    }

    private void BikeTrainingAction()
    {
        Entity(BikeFanSwitch!).TurnOn();
    }

    private void NoBikeTrainingAction()
    {
        Entity(BikeFanSwitch!).TurnOff();
    }

    protected override bool IndoorRoom => true;
    protected override TimeSpan OccupancyTimeout => TimeSpan.FromMinutes(10);
}