using System.Collections.Generic;
using System.Threading.Tasks;
using JoySoftware.HomeAssistant.NetDaemon.Common;
using System.Linq;
using System;
using Microsoft.Extensions.Logging;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;


public abstract class RoomApp : NetDaemonApp
{
    protected virtual TimeSpan OccupancyTimeout => TimeSpan.FromMinutes(3);
    protected virtual TimeSpan PowerSensorOffDebounce => TimeSpan.FromMinutes(5);
    protected virtual TimeSpan PowerSensorOnDebounce => TimeSpan.FromSeconds(30);
    protected virtual TimeSpan MediaPlayerStopDebounce => TimeSpan.FromMinutes(1);

    protected abstract bool IndoorRoom { get; }

    public IEnumerable<string>? MotionSensors { get; set; }
    public IEnumerable<string>? PowerSensors { get; set; }
    public IEnumerable<string>? MediaPlayerDevices { get; set; }
    public IEnumerable<string>? EntryPoints { get; set; }
    
    protected virtual IEnumerable<string>? AllOccupancySensors => MotionSensors.Union(PowerSensors);

    public IEnumerable<string>? Lights { get; set; }

    protected ISchedulerResult? Timer;

    protected virtual bool DebugLogEnabled => false;

    protected virtual bool MotionEnabled => IndoorRoom ?
        GetState("input_boolean.indoor_motion_enabled")?.State == "on" :
        GetState("input_boolean.outdoor_motion_enabled")?.State == "on";

    public override Task InitializeAsync()
    {
        MotionSensors ??= new List<string>();
        PowerSensors ??= new List<string>();
        MediaPlayerDevices ??= new List<string>();
        Lights ??= new List<string>();
        EntryPoints ??= new List<string>();

        SetupOccupied();
        SetupUnoccupied();

        return Task.CompletedTask;
    }

    #region Triggers

    private void SetupUnoccupied()
    {
        if (MotionSensors != null && MotionSensors.Any())
        {
            Entities(MotionSensors)
                .WhenStateChange((to, from) => @from?.State == "on" && to?.State == "off" && MotionEnabled)
                .Call(NoPresenceAction)
                .Execute();
        }

        if (Lights != null && Lights.Any())
        {
            Entities(Lights!)
                .WhenStateChange(@from: "on", to: "off")
                .Call(NoPresenceAction)
                .Execute();
        }

        if (PowerSensors != null && PowerSensors.Any())
        {
            Entities(PowerSensors)
                .WhenStateChange(from: "on", to: "off")
                .AndNotChangeFor(PowerSensorOffDebounce)
                .Call(NoPresenceAction)
                .Execute();
        }

        if (MediaPlayerDevices != null && MediaPlayerDevices.Any())
        {
            Entities(MediaPlayerDevices)
                .WhenStateChange((from,to) => new List<string>{"idle", "paused"}.Contains(from!.State) && to!.State == "playing")
                .AndNotChangeFor(MediaPlayerStopDebounce)
                .Call(NoPresenceAction)
                .Execute();
        }

        if (EntryPoints != null && EntryPoints.Any())
        {
            Entities(EntryPoints!)
                .WhenStateChange((from, to) => 
                    new List<string>{"on", "closed"}.Contains(from!.State) &&
                    new List<string> { "off", "open" }.Contains(to!.State))
                .Call(NoPresenceAction)
                .Execute();
        }
    }

    private void SetupOccupied()
    {
        if (MotionSensors != null && MotionSensors.Any())
        {
            Entities(MotionSensors!)
                .WhenStateChange((to, from) => @from?.State == "off" && to?.State == "on" && MotionEnabled)
                .Call(PresenceAction)
                .Execute();
        }

        if (Lights != null && Lights.Any())
        {
            Entities(Lights!)
                .WhenStateChange(@from: "off", to: "on")
                .Call(PresenceAction)
                .Execute();
        }

        if (PowerSensors != null && PowerSensors.Any())
        {
            Entities(PowerSensors)
                .WhenStateChange(from: "off", to: "on")
                .AndNotChangeFor(PowerSensorOnDebounce)
                .Call(PresenceAction)
                .Execute();
        }

        if (MediaPlayerDevices != null && MediaPlayerDevices.Any())
        {
            Entities(MediaPlayerDevices)
                .WhenStateChange((from, to) => from!.State == "playing" && new List<string> { "idle", "paused" }.Contains(to!.State))
                .AndNotChangeFor(MediaPlayerStopDebounce)
                .Call(PresenceAction)
                .Execute();
        }

        if (EntryPoints != null && EntryPoints.Any())
        {
            Entities(EntryPoints!)
                .WhenStateChange((from,to) =>new List<string> { "off", "open" }.Contains(from!.State) &&
                                 new List<string> { "on", "closed" }.Contains(to!.State))
                .Call(PresenceAction)
                .Execute();
        }
    }
    #endregion



    private async Task NoPresenceAction(string entityId, EntityState? to, EntityState? from)
    {
        DebugLog( $"No Presence: {entityId}", entityId);

        foreach (var os in AllOccupancySensors!.Union(MediaPlayerDevices!))
        {
            DebugLog( "{os} : {state}", os, GetState(os!).State);
        }

        DebugLog( "Timer is empty: {timer}", Timer == null);

        DebugLog("all occupancy are off: {result}", this.AllStatesAre(AllOccupancySensors, "off", "closed"));

        if ((AllOccupancySensors == null || !AllOccupancySensors.Any() || this.AllStatesAre(AllOccupancySensors, "off", "closed")) && 
            (MediaPlayerDevices == null || !MediaPlayerDevices.Any() || !this.AnyStatesAre(MediaPlayerDevices, "playing")) && 
            Timer == null)
        {
            DebugLog("calling no presence action");
            await NoPresenceAction();
        }
        else
        {

            DebugLog("No presence criteria not met");
            //DebugLog("occupancy== null: {result}", AllOccupancySensors == null);
            //DebugLog("alloccupancy any: {result}", AllOccupancySensors.Any());
            //DebugLog("all occupancy off/closed: {result}", this.AllStatesAre(AllOccupancySensors, "off", "closed"));
            //DebugLog("mediadevices == null: {result}", MediaPlayerDevices != null);
            //DebugLog("Timer == null: {result}", Timer == null);
        }
    }

    protected virtual async Task NoPresenceAction()
    {
        await ToggleLights(false);
    }

    private async Task PresenceAction(string entityId, EntityState? to, EntityState? from)
    {
        DebugLog( $"Presence: {entityId}", entityId);
        await PresenceAction();
    }

    protected virtual async Task PresenceAction()
    {
        await ToggleLights(true);
    }

    public void StartTimer()
    {
        CancelTimer();
        Timer = Scheduler.RunIn(OccupancyTimeout, () =>
        {
            Timer = null;
            return NoPresenceAction("timer", null, null);
        });
    }

    public void CancelTimer()
    {
        Timer?.CancelSource.Cancel();
        Timer = null;
    }

    [DisableLog(SupressLogType.MissingExecute)]
    public async Task ToggleLights(bool on)
    {
        DebugLog( "Toggle lights: {on}", on);

        if (Lights != null && Lights.Any())
        {
            var lights = Entities(Lights);

            IAction? action = null;
            if (@on)
            {
                if (!this.AllStatesAre(Lights, "on"))
                {
                    // todo: lux sensitivity
                    action = lights.TurnOn();
                }

                StartTimer();
            }
            else if (!on)
            {
                if (!this.AllStatesAre(Lights, "off"))
                {
                    action = lights.TurnOff();
                }

                CancelTimer();
            }

            if (action != null)
                await action.ExecuteAsync();
        }
    }

    protected void DebugLog(string message, params object[] data)
    {
        if (DebugLogEnabled)
            Log(LogLevel.Information, message, data);
    }
}