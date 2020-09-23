using System;
using System.Reactive.Linq;
using JetBrains.Annotations;
using NetDaemon.Common.Reactive;

[UsedImplicitly]
public class Gym : RoomApp
{
    private const string Training = "sensor.trainer_wattage";
    private const string Climate = "sensor.gym_temperature";
    private const string BikeFanSwitch = "switch.gym_bike_fan";
    private const string WeightFanSwitch = "switch.gym_weights_fan";
    private const string FanButton = "sensor.gym_switch_click";
    private const double FanTriggerTemp = 25;

    public override void Initialize()
    {
        Entity(Training!)
            .StateChangesFiltered()
            .DistinctUntilChanged(s => (s.New!.State ?? 0L) >= s.New.Attribute!.active_threshold)
            .Where(s => (s.New!.State ?? 0L) >= s.New.Attribute!.active_threshold && State(Climate!)!.State >= FanTriggerTemp)
            .NDSameStateFor(TimeSpan.FromMinutes(1))
            .Subscribe(_ =>
            {
                var training = State(Training!)!;

                if (training.State >= training.Attribute!.active_threshold)
                {
                    LogHistory("Bike training");
                    BikeTrainingAction();
                }
            });


        Entity(Climate!)
            .StateChangesFiltered()
            .DistinctUntilChanged(s => 
                (double?)s.Old!.State! < FanTriggerTemp &&
                (double?)s.New!.State! >= FanTriggerTemp)
            .Where(s =>
            {
                var fantrigger = (double?) s.Old!.State! < FanTriggerTemp &&
                                 (double?) s.New!.State! >= FanTriggerTemp;

                var training = State(Training!)!;

                return fantrigger && training.State >= training.Attribute!.active_threshold;

            })
            .Subscribe(_ =>
            {
                var training = State(Training!)!;
                if (training.State >= training.Attribute!.active_threshold)
                {
                    LogHistory("Bike training climate");
                    BikeTrainingAction();
                }
            });


        Entity(Training!)
            .StateChangesFiltered()
            .FilterDistinctUntilChanged(s =>
                (s.New!.State ?? 0L) < s.New.Attribute!.active_threshold)
            .NDSameStateFor(TimeSpan.FromMinutes(2))
            .Subscribe(_ =>
            {
                var training = State(Training!)!;
                if (training.State < training.Attribute!.active_threshold)
                {
                    LogHistory("Bike training stopped");
                    NoBikeTrainingAction();
                }
            });

        Entity(FanButton!)
            .StateChangesFiltered()
            .Where(s => s.New.State == "left")
            .Subscribe(_ =>
            {
                LogHistory("Bike fan toggle");
                ToggleBikeFan();
            });

        Entity(FanButton!)
          .StateChangesFiltered()
            .Where(s => s.New.State == "right")
            .Subscribe(_ =>
          {
              LogHistory("Weight fan toggle");
              ToggleWeightFan();
          });

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