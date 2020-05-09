using System.Collections.Generic;
using System.Threading.Tasks;
using JoySoftware.HomeAssistant.NetDaemon.Common;
using System.Linq;
using System;
using System.ComponentModel.DataAnnotations;
using EnumsNET;
using Humanizer;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;


public abstract class RoomApp : NetDaemonApp
{
    protected virtual string RoomPrefix => GetType().Name;

    private TimeSpan OccupancyTimeoutTest => TimeSpan.FromMinutes(2);

    protected virtual TimeSpan OccupancyTimeout => TimeSpan.FromMinutes(3);

    protected TimeSpan OccupancyTimeoutObserved => DebugMode ? OccupancyTimeoutTest : OccupancyTimeout;
    protected virtual TimeSpan PowerSensorOffDebounce => TimeSpan.FromMinutes(5);
    protected virtual TimeSpan PowerSensorOnDebounce => TimeSpan.FromSeconds(30);
    protected virtual TimeSpan MediaPlayerStopDebounce => TimeSpan.FromMinutes(1);
    protected virtual TimeSpan WorkstationDebounce => TimeSpan.FromMinutes(1);

    protected abstract bool IndoorRoom { get; }

    public Func<IEntityProperties, bool> MotionSensors => e => IsEntityMatch(e, EntityType.BinarySensor, DeviceClass.Motion);
    public Func<IEntityProperties, bool> OccupancySensors => e => IsEntityMatch(e, EntityType.BinarySensor, DeviceClass.Occupancy);
    public Func<IEntityProperties, bool> PowerSensors => e => IsEntityMatch(e, EntityType.Sensor, DeviceClass.Power) && e.Attribute!.active_threshold != null;
    public Func<IEntityProperties, bool> MediaPlayerDevices => e => IsEntityMatch(e, EntityType.MediaPlayer);
    public Func<IEntityProperties, bool> Lights => e => IsEntityMatch(e, EntityType.Light);
    public Func<IEntityProperties, bool> Workstations => e => IsEntityMatch(e, EntityType.WorkStation);
    public Func<IEntityProperties, bool> EntryPoints =>
        e => IsEntityMatch(e, EntityType.BinarySensor, DeviceClass.Door, DeviceClass.Window) ||
                           IsEntityMatch(e, EntityType.Cover, DeviceClass.Garage);
    protected virtual Func<IEntityProperties, bool> AllOccupancySensors => e => MotionSensors(e) || OccupancySensors(e);

    protected Func<IEntityProperties, bool> MasterOffSwitches =>  e => IsEntityMatch(e, EntityType.Sensor) && e.Attribute!.switch_type == SwitchType.MasterOff.AsString(EnumFormat.DisplayName, EnumFormat.Name)!.ToLower();

    protected ISchedulerResult? Timer;
    public DateTime? TimerEndDate { get; set; }

    protected virtual bool DebugMode => false;

    protected virtual bool AutoDiscoverDevices => true;

    protected virtual bool PresenceLightingEnabled => IndoorRoom ?
        GetState("input_boolean.indoor_motion_enabled")?.State == "on" :
        GetState("input_boolean.outdoor_motion_enabled")?.State == "on";

    public override Task InitializeAsync()
    {
        if (AutoDiscoverDevices)
        {
            LogDiscoveredEntities();
            DebugLog("Occupancy Timeout: {timeout} minute(s)", OccupancyTimeoutObserved.TotalMinutes);

            SetupOccupied();
            SetupUnoccupied();

            Entities(MasterOffSwitches).WhenStateChange(to: "single")
                .Call(TurnEveryThingOff)
                .Execute();
        }
        else
        {
            Log(LogLevel.Warning, "{Room} auto discovery is disabled", RoomPrefix);
        }

        // Start a timer so that if restarting netdaemon (and existing timers are lost) and lights are on
        // they will get turned off 
        StartTimer();

        return Task.CompletedTask;
    }

    private async Task TurnEveryThingOff(string arg1, EntityState? arg2, EntityState? arg3)
    {
        DebugLog("Turning everything off");
        await Entities(e => e.EntityId.StartsWith("light.")).TurnOff().ExecuteAsync();
    }

    private void LogDiscoveredEntities()
    {
        if (!DebugMode) return;

        DebugEntityDiscovery(MotionSensors, nameof(MotionSensors));
        DebugEntityDiscovery(PowerSensors, nameof(PowerSensors));
        DebugEntityDiscovery(MediaPlayerDevices, nameof(MediaPlayerDevices));
        DebugEntityDiscovery(Lights, nameof(Lights));
        DebugEntityDiscovery(EntryPoints, nameof(EntryPoints));
        DebugEntityDiscovery(Workstations, nameof(Workstations));
        DebugEntityDiscovery(OccupancySensors, nameof(OccupancySensors));
        DebugEntityDiscovery(MasterOffSwitches, nameof(MasterOffSwitches));
    }

    private void DebugEntityDiscovery(Func<IEntityProperties, bool> searcher, string description)
    {
        var humanDescription = description.Humanize(LetterCasing.LowerCase);
        DebugLog("Searching for {description}", humanDescription);

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
        Entities(OccupancySensors)
            .WhenStateChange((to, from) => @from?.State == "on" && to?.State == "off")
            .Call(StartOccupancyTimer)
            .Execute();

        Entities(Workstations)
            .WhenStateChange(to: "off", from: "on")
            .AndNotChangeFor(WorkstationDebounce)
            .Call(StartOccupancyTimer)
            .Execute();

        Entities(PowerSensors)
            .WhenStateChange((to, from) =>
                from!.State >= State.Single(s => s.EntityId == to!.EntityId!).Attribute!.active_threshold &&
                to!.State < State.Single(s => s.EntityId == to.EntityId!).Attribute!.active_threshold)
            .AndNotChangeFor(PowerSensorOffDebounce)
            .Call(StartOccupancyTimer)
            .Execute();

        Entities(MediaPlayerDevices)
            .WhenStateChange((to, from) =>
                from!.State == "playing" &&
                new List<string> { "idle", "paused" }.Contains(to!.State))
            .AndNotChangeFor(MediaPlayerStopDebounce)
            .Call(StartOccupancyTimer)
            .Execute();

        // This ensures if a room has motion continually for some time that we start timers etc
        // from the point at which motion ceases
        Entities(MotionSensors)
            .WhenStateChange((to, from) => @from?.State == "on" && to?.State == "off")
            .Call(StartOccupancyTimer)
            .Execute();

        Entities(EntryPoints)
            .WhenStateChange((to, from) =>
                new List<string> { "on", "open" }.Contains(from!.State) &&
                new List<string> { "off", "closed" }.Contains(to!.State))
            .Call(StartOccupancyTimer)
            .Execute();
    }

    private void SetupOccupied()
    {
        Entities(MotionSensors)
            .WhenStateChange((to, from) => @from?.State == "off" && to?.State == "on")
            .Call(PresenceAction)
            .Execute();

        Entities(OccupancySensors)
            .WhenStateChange((to, from) => @from?.State == "off" && to?.State == "on")
            .Call(PresenceAction)
            .Execute();

        Entities(Lights)
            .WhenStateChange(to: "on", from: "off")
            .Call(PresenceAction)
            .Execute();

        Entities(Workstations)
            .WhenStateChange(to: "on", from: "off")
            .Call(PresenceAction)
            .Execute();

        Entities(PowerSensors)
            .WhenStateChange((to, from) =>
                from!.State < State.Single(s => s.EntityId == to!.EntityId!).Attribute!.active_threshold &&
                to!.State >= State.Single(s => s.EntityId == to.EntityId!).Attribute!.active_threshold)
            .AndNotChangeFor(PowerSensorOnDebounce)
            .Call(PresenceAction)
            .Execute();

        Entities(MediaPlayerDevices)
            .WhenStateChange((to, from) =>
                new List<string> { "idle", "paused" }.Contains(from!.State) &&
                to!.State == "playing")
            .AndNotChangeFor(MediaPlayerStopDebounce)
            .Call(PresenceAction)
            .Execute();

        Entities(EntryPoints)
            .WhenStateChange((to, from) =>
                new List<string> { "off", "closed" }.Contains(from!.State) &&
                new List<string> { "on", "open" }.Contains(to!.State))
            .Call(PresenceAction)
            .Execute();
    }

    private async Task StartOccupancyTimer(string entityId, EntityState? to, EntityState? from)
    {
        DebugLog("Starting occupancy timer due to: {entityId} from {fromState} to {toState}", entityId, from?.State, to?.State);
        StartTimer();

        await Task.CompletedTask;
    }

    #endregion

    private async Task NoPresenceAction(string entityId, EntityState? to, EntityState? from)
    {
        DebugLog("No Presence: {entityId} from {fromState} to {toState}", entityId, from?.State, to?.State);

        foreach (var os in State.Where(e => AllOccupancySensors(e) || MediaPlayerDevices(e) || Workstations(e)))
        {
            DebugLog("{os}:{state}", os.EntityId, os.State);
        }

        foreach (var ps in State.Where(e => PowerSensors(e)))
        {
            DebugLog("{ps}:{state}W - Threshold {threshold}W - Above threshold: {threshholdmet}", ps.EntityId, ps.State,
                ps.Attribute!.active_threshold, ps.State >= ps.Attribute.active_threshold);
        }

        DebugLog("Occupancy timer is running: {timer}", Timer != null);

        if (TimerEndDate != null)
            DebugLog("Occupancy timer will finish at {date} (seconds)", TimerEndDate.Value,
                TimerEndDate.Value - DateTime.Now);

        if (this.AllStatesAre(AllOccupancySensors, "off", "closed") &&
            !this.AnyStatesAre(MediaPlayerDevices, "playing") &&
            !this.AnyStatesAre(Workstations, "home") &&
            !this.AnyStatesAre(PowerSensors, p => p.State >= p.Attribute!.active_threshold) &&
            Timer == null)
        {
            DebugLog("Calling no presence action");
            await NoPresenceAction();
        }
        else
        {
            DebugLog("No presence criteria not met");
        }
    }

    protected virtual async Task NoPresenceAction()
    {
        CancelTimer();
        await ToggleLights(false);
    }

    private async Task PresenceAction(string entityId, EntityState? to, EntityState? from)
    {
        DebugLog("Presence: {entityId} from {fromState} to {toState}", entityId, from?.State, to?.State);
        await PresenceAction();
    }

    protected virtual async Task PresenceAction()
    {
        StartTimer();
        await ToggleLights(true);
    }

    public void StartTimer()
    {
        CancelTimer();

        TimerEndDate = DateTime.Now.Add(OccupancyTimeoutObserved);
        DebugLog("Timer scheduled for {Timer}", TimerEndDate);
        Timer = Scheduler.RunIn(OccupancyTimeoutObserved, () =>
        {
            
            Timer = null;
            TimerEndDate = null;
            return NoPresenceAction("timer", new EntityState{State = "completed"}, new EntityState { State = "active" });
        });
    }

    public void CancelTimer()
    {
        if (Timer != null)
        {
            DebugLog("Cancelling timer");
        }

        Timer?.CancelSource.Cancel();
        Timer = null;
        TimerEndDate = null;
    }

    [DisableLog(SupressLogType.MissingExecute)]
    public async Task ToggleLights(bool on)
    {
        if (!PresenceLightingEnabled)
        {
            DebugLog("Presence lighting disabled, not settings lights to: {state}", on ? "on" : "off");
            return;
        }

        DebugLog("Toggle lights: {on}", on);

        var lights = Entities(Lights);

        IAction? action = null;
        if (@on)
        {
            if (!this.AllStatesAre(Lights, "on"))
            {
                action = lights.TurnOn();
            }
        }
        else if (!on)
        {
            if (!this.AllStatesAre(Lights, "off"))
            {
                action = lights.TurnOff();
            }
        }

        if (action != null)
            await action.ExecuteAsync();
    }

    protected void DebugLog(string message, params object[] data)
    {
        if (DebugMode)
            Log(LogLevel.Information, $"{Guid.NewGuid()} - {message}", data);
    }

    private bool IsEntityMatch(IEntityProperties prop, EntityType entityType, params DeviceClass[] deviceClasses)
    {
        var entityString = entityType.AsString(EnumFormat.DisplayName, EnumFormat.Name)!.ToLower();
        var deviceStrings = deviceClasses.Select(t => t.AsString(EnumFormat.DisplayName, EnumFormat.Name)!.ToLower()).ToList();

        var areas = prop.Attribute?.area;

        if (areas == null)
            return false;

        if (!((string)areas).Split(",").Contains(RoomPrefix.ToLower()))
            return false;

        if (prop.EntityId.ToLower().Split(".")[0] != entityString)
            return false;

        if (deviceStrings == null || !deviceStrings.Any())
            return true;

        return deviceStrings.Contains(prop.Attribute?.device_class);
    }
}

public enum EntityType
{
    [Display(Name = "binary_sensor")]
    BinarySensor,
    Light,
    Switch,
    [Display(Name = "media_player")]
    MediaPlayer,
    Sensor,
    Cover,
    [Display(Name = "device_tracker")]
    WorkStation
}

public enum DeviceClass
{
    Motion,
    Power,
    Door,
    Window,
    Garage,
    Occupancy
}

public enum SwitchType
{
    MasterOff
}

