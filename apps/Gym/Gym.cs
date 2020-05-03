using System;
using System.Threading.Tasks;
using JoySoftware.HomeAssistant.NetDaemon.Common;

public class Gym : RoomApp
{
    public string? Training { get; set; }
    public string? Climate { get; set; }
    public string? BikeFanSwitch { get; set; }
    public string? WeightFanSwitch { get; set; }
    public string? FanButton { get; set; }

    private const int FanTriggerTemp = 25;

    public override Task InitializeAsync()
    {
        Entity(Training!)
            .WhenStateChange((from, to) => 
                from!.State == "off" && to!.State == "on" && 
                GetState(Climate!)!.State >= FanTriggerTemp)
            .AndNotChangeFor(TimeSpan.FromMinutes(1))
            .Call(BikeTrainingAction)
            .Execute();

        Entity(Climate!)
            .WhenStateChange((from, to) =>
                from!.State < FanTriggerTemp &&
                to!.State >= FanTriggerTemp &&
                GetState(Training!)!.State == "on")
            .Call(BikeTrainingAction)
            .Execute();

        Entity(Training!)
            .WhenStateChange(from: "on", to: "off")
            .AndNotChangeFor(TimeSpan.FromMinutes(2))
            .Call(NoBikeTrainingAction)
            .Execute();

        Entity(FanButton!)
            .WhenStateChange(to: "left")
            .Call(ToggleBikeFan)
            .Execute();

        Entity(FanButton!)
            .WhenStateChange(to: "right")
            .Call(ToggleWeightFan)
            .Execute();

        return base.InitializeAsync();
    }

    private async Task ToggleWeightFan(string arg1, EntityState? arg2, EntityState? arg3)
    {
        await Entity(WeightFanSwitch!).Toggle().ExecuteAsync();
    }

    private async Task ToggleBikeFan(string arg1, EntityState? arg2, EntityState? arg3)
    {
        await Entity(BikeFanSwitch!).Toggle().ExecuteAsync();
    }

    private async Task BikeTrainingAction(string arg1, EntityState? arg2, EntityState? arg3)
    {
        await Entity(BikeFanSwitch!).TurnOn().ExecuteAsync();
    }

    private async Task NoBikeTrainingAction(string arg1, EntityState? arg2, EntityState? arg3)
    {
        await Entity(BikeFanSwitch!).TurnOff().ExecuteAsync();
    }

    protected override bool IndoorRoom => true;
    protected override TimeSpan OccupancyTimeout => TimeSpan.FromMinutes(10);
    protected override bool DebugLogEnabled => true;
}