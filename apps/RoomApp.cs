using System.Collections.Generic;
using System.Threading.Tasks;
using JoySoftware.HomeAssistant.NetDaemon.Common;
using System.Linq;
using System;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using System.Threading;
using EnumsNET;
using Humanizer;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;


public abstract class RoomApp : NetDaemonApp
{
    protected virtual string RoomPrefix => GetType().Name;

    protected virtual TimeSpan OccupancyTimeout => TimeSpan.FromMinutes(3);
    protected virtual TimeSpan PowerSensorOffDebounce => TimeSpan.FromMinutes(5);
    protected virtual TimeSpan PowerSensorOnDebounce => TimeSpan.FromSeconds(30);
    protected virtual TimeSpan MediaPlayerStopDebounce => TimeSpan.FromMinutes(1);

    protected abstract bool IndoorRoom { get; }

    public Func<IEntityProperties, bool> MotionSensors => e => MotionSensorsRegex.IsMatch(e.EntityId);

    public Regex MotionSensorsRegex => new Regex(GetEntityRegex(EntityType.BinarySensor, DeviceType.Motion),
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public Func<IEntityProperties, bool> PowerSensors => e => PowerSensorsRegex.IsMatch(e.EntityId) &&  e.Attribute!.active_threshold != null;

    public Regex PowerSensorsRegex => new Regex(GetEntityRegex(EntityType.Sensor, DeviceType.Wattage),RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public Func<IEntityProperties, bool> MediaPlayerDevices => e => MediaPlayerDevicesRegex.IsMatch(e.EntityId);
    public Regex MediaPlayerDevicesRegex => new Regex(GetEntityRegex(EntityType.MediaPlayer), RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public Func<IEntityProperties, bool> Lights => e => LightsRegex.IsMatch(e.EntityId);
    public Regex LightsRegex => new Regex(GetEntityRegex(EntityType.Light), RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public Func<IEntityProperties, bool> EntryPoints => e => EntryPointsRegex.IsMatch(e.EntityId);
    public Regex EntryPointsRegex => new Regex(GetEntityRegex(EntityType.BinarySensor, DeviceType.Door, DeviceType.Window), RegexOptions.Compiled | RegexOptions.IgnoreCase);
    protected virtual Func<IEntityProperties, bool> AllOccupancySensors => e => MotionSensors(e) && PowerSensors(e);

    protected ISchedulerResult? Timer;

    protected virtual bool DebugLogEnabled => false;

    protected virtual bool MotionEnabled => IndoorRoom ?
        GetState("input_boolean.indoor_motion_enabled")?.State == "on" :
        GetState("input_boolean.outdoor_motion_enabled")?.State == "on";

    public override Task InitializeAsync()
    {
        LogDiscoveredEntities();

        SetupOccupied();
        SetupUnoccupied();

        return Task.CompletedTask;
    }

    private void LogDiscoveredEntities()
    {
        if (!DebugLogEnabled) return;

        DebugEntityDiscovery(MotionSensors, nameof(MotionSensors), MotionSensorsRegex);
        DebugEntityDiscovery(PowerSensors, nameof(PowerSensors), PowerSensorsRegex);
        DebugEntityDiscovery(MediaPlayerDevices, nameof(MediaPlayerDevices), MediaPlayerDevicesRegex);
        DebugEntityDiscovery(Lights, nameof(Lights), LightsRegex);
        DebugEntityDiscovery(EntryPoints, nameof(EntryPoints), EntryPointsRegex);
    }

    private void DebugEntityDiscovery(Func<IEntityProperties, bool> searcher, string description, Regex searchRegex)
    {
        var humanDescription = description.Humanize(LetterCasing.LowerCase);
        DebugLog("Searching for {description} using regex '{regex}'", humanDescription, searchRegex.ToString());

        var states = State.Where(searcher).ToList();
        
        DebugLog("{count} {description} found", states.Count, humanDescription);

        foreach (var entity in states)
        {
            DebugLog("Found {description}: {entity}", humanDescription, entity.EntityId);
        }
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
            .WhenStateChange((from, to) => to!.State < State.Single(s => s.EntityId == to.EntityId!).Attribute!.active_threshold)
            //.WhenStateChange(from: "on", to: "off")
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
            .WhenStateChange((from, to) => to!.State >= State.Single(s => s.EntityId == to.EntityId!).Attribute!.active_threshold)
            //.WhenStateChange(from: "off", to: "on")
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
            Log(LogLevel.Information, message, data);
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

        var entityRegex = @$"{entityString}\.{RoomPrefix}(?:_[A-Za-z0-9]*)*{deviceString}(?:_\d)*$".ToLower();

        return entityRegex;
    }

    private enum EntityType
    {
        [Display(Name = "binary_sensor")]
        BinarySensor,
        Light,
        Switch,
        [Display(Name = "media_player")]
        MediaPlayer,
        Sensor
    }

    private enum DeviceType
    {
        Motion,
        Power,
        Door,
        Window,
        Wattage
    }
}

