using System.Collections.Generic;
using System.Threading.Tasks;
using JoySoftware.HomeAssistant.NetDaemon.Common;
using System.Linq;
using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using System.Threading;
using EnumsNET;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;


public abstract class RoomApp : NetDaemonApp
{
    protected abstract string RoomPrefix { get; }

    protected virtual TimeSpan OccupancyTimeout => TimeSpan.FromMinutes(3);
    protected virtual TimeSpan PowerSensorOffDebounce => TimeSpan.FromMinutes(5);
    protected virtual TimeSpan PowerSensorOnDebounce => TimeSpan.FromSeconds(30);
    protected virtual TimeSpan MediaPlayerStopDebounce => TimeSpan.FromMinutes(1);

    protected abstract bool IndoorRoom { get; }

    public Func<IEntityProperties, bool> MotionSensors => e => Regex.Match(e.EntityId, GetEntityRegex(EntityType.BinarySensor, DeviceType.Motion)).Success;
    public Func<IEntityProperties, bool> PowerSensors => e => Regex.Match(e.EntityId, GetEntityRegex(EntityType.Switch, DeviceType.Power)).Success;

    public Func<IEntityProperties, bool> MediaPlayerDevices => e => Regex.Match(e.EntityId, GetEntityRegex(EntityType.MediaPlayer)).Success;
    public Func<IEntityProperties, bool> Lights => e => Regex.Match(e.EntityId, GetEntityRegex(EntityType.Light)).Success;

    public Func<IEntityProperties, bool> EntryPoints => e => Regex.Match(e.EntityId, GetEntityRegex(EntityType.BinarySensor, DeviceType.Door, DeviceType.Window)).Success;
    protected virtual Func<IEntityProperties, bool> AllOccupancySensors => e => MotionSensors(e) && PowerSensors(e);

    protected ISchedulerResult? Timer;

    protected virtual bool DebugLogEnabled => false;

    protected virtual bool MotionEnabled => IndoorRoom ?
        GetState("input_boolean.indoor_motion_enabled")?.State == "on" :
        GetState("input_boolean.outdoor_motion_enabled")?.State == "on";

    public override Task InitializeAsync()
    {
        SetupOccupied();
        SetupUnoccupied();

        return Task.CompletedTask;
    }

    #region Triggers

    private void SetupUnoccupied()
    {
        Entities(MotionSensors)
            .WhenStateChange((to, from) => @from?.State == "on" && to?.State == "off" && MotionEnabled)
            .Call(NoPresenceAction)
            .Execute();

        Entities(Lights)
            .WhenStateChange(@from: "on", to: "off")
            .Call(NoPresenceAction)
            .Execute();

        Entities(PowerSensors)
            .WhenStateChange(from: "on", to: "off")
            .AndNotChangeFor(PowerSensorOffDebounce)
            .Call(NoPresenceAction)
            .Execute();

        Entities(MediaPlayerDevices)
            .WhenStateChange((from, to) =>
                new List<string> { "idle", "paused" }.Contains(from!.State) && to!.State == "playing")
            .AndNotChangeFor(MediaPlayerStopDebounce)
            .Call(NoPresenceAction)
            .Execute();

        Entities(EntryPoints)
            .WhenStateChange((from, to) =>
                new List<string> { "on", "closed" }.Contains(from!.State) &&
                new List<string> { "off", "open" }.Contains(to!.State))
            .Call(NoPresenceAction)
            .Execute();
    }

    private void SetupOccupied()
    {
        Entities(MotionSensors)
            .WhenStateChange((to, from) => @from?.State == "off" && to?.State == "on" && MotionEnabled)
            .Call(PresenceAction)
            .Execute();

        Entities(Lights)
            .WhenStateChange(@from: "off", to: "on")
            .Call(PresenceAction)
            .Execute();

        Entities(PowerSensors)
            .WhenStateChange(from: "off", to: "on")
            .AndNotChangeFor(PowerSensorOnDebounce)
            .Call(PresenceAction)
            .Execute();

        Entities(MediaPlayerDevices)
            .WhenStateChange((from, to) =>
                from!.State == "playing" && new List<string> { "idle", "paused" }.Contains(to!.State))
            .AndNotChangeFor(MediaPlayerStopDebounce)
            .Call(PresenceAction)
            .Execute();

        Entities(EntryPoints)
            .WhenStateChange((from, to) => new List<string> { "off", "open" }.Contains(from!.State) &&
                                           new List<string> { "on", "closed" }.Contains(to!.State))
            .Call(PresenceAction)
            .Execute();
    }

    #endregion

    private async Task NoPresenceAction(string entityId, EntityState? to, EntityState? from)
    {
        DebugLog($"No Presence: {entityId}", entityId);

        foreach (var os in State.Where(e => AllOccupancySensors(e) || MediaPlayerDevices(e)))
        {
            DebugLog("{os} : {state}", os.EntityId, os.State);
        }

        DebugLog("Timer is empty: {timer}", Timer == null);

        if (this.AllStatesAre(AllOccupancySensors, "off", "closed") &&
            !this.AnyStatesAre(MediaPlayerDevices, "playing") &&
            Timer == null)
        {
            DebugLog("calling no presence action");
            await NoPresenceAction();
        }
        else
        {
            DebugLog("No presence criteria not met");
        }
    }

    protected virtual async Task NoPresenceAction()
    {
        await ToggleLights(false);
    }

    private async Task PresenceAction(string entityId, EntityState? to, EntityState? from)
    {
        DebugLog($"Presence: {entityId}", entityId);
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
        DebugLog("Toggle lights: {on}", on);

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

    protected void DebugLog(string message, params object[] data)
    {
        if (DebugLogEnabled)
            Log(LogLevel.Information, $"{Thread.CurrentThread.Name}: {message}", data);
    }

    private string GetEntityRegex(EntityType entityType, params DeviceType[] deviceTypes)
    {
        var entityString = entityType.AsString(EnumFormat.DisplayName, EnumFormat.Name);

        string? deviceString = null;

        if (deviceTypes.Any())
            deviceString = string.Join("|",deviceTypes.Select(d => d.AsString(EnumFormat.DisplayName, EnumFormat.Name)));

        if (!string.IsNullOrEmpty(deviceString))
            deviceString = $"_({deviceString})";

        // light.study_1
        // binary_sensor.study_motion
        // binary_sensor.study_motion_1
        // binary_sensor.study_door
        // binary_sensor.study_door_1
        // binary_sensor.study_window
        // binary_sensor.study_window_1

        var entityRegex = @$"{entityString}.{RoomPrefix}{deviceString}(_\d)*";

        return entityRegex;
    }

    private enum EntityType
    {
        [Display(Name = "binary_sensor")]
        BinarySensor,
        Light,
        Switch,
        MediaPlayer
    }

    private enum DeviceType
    {
        Motion,
        Power,
        Door,
        Window
    }
}