using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reactive.Linq;
using daemonapp.Utilities;
using EnumsNET;
using Humanizer;
using Microsoft.Extensions.Logging;
using NetDaemon.Common;
using NetDaemon.Common.Fluent;
using NetDaemon.Common.Reactive;

public abstract class RoomApp : NetDaemonRxApp
{
    // ReSharper disable once RedundantLogicalConditionalExpressionOperand
    protected virtual bool DebugMode => SingleRoomModeName == RoomName || false;

    private readonly string? SingleRoomModeName = "";

    protected abstract bool IndoorRoom { get; }
    protected virtual bool AutomatedLightsOn => IndoorRoom ?
        State("input_boolean.indoor_motion_enabled")?.State == "on" :
        State("input_boolean.outdoor_motion_enabled")?.State == "on";

    protected virtual bool AutomatedLightsOff => true;

    protected virtual bool AutoDiscoverDevices => true;

    protected virtual bool SecondaryLightingEnabled => false;

    protected virtual Dictionary<string, object>? SecondaryLightingAttributes => null;
    protected virtual string RoomName => GetType().Name;

    protected TimeSpan OccupancyTimeoutObserved => DebugMode ? OccupancyTimeoutTest : OccupancyTimeout;

    private TimeSpan OccupancyTimeoutTest => TimeSpan.FromMinutes(1);

    protected virtual TimeSpan OccupancyTimeout => TimeSpan.FromMinutes(3);

    public Func<IEntityProperties, bool> MotionSensors => e => IsEntityMatch(e, EntityType.BinarySensor, DeviceClass.Motion);
    public Func<IEntityProperties, bool> OccupancySensors => e => IsEntityMatch(e, EntityType.BinarySensor, DeviceClass.Occupancy);
    public Func<IEntityProperties, bool> PowerSensors => e => IsEntityMatch(e, EntityType.Sensor, DeviceClass.Power) && e.Attribute!.active_threshold != null;
    public Func<IEntityProperties, bool> MediaPlayerDevices => e => IsEntityMatch(e, EntityType.MediaPlayer);
    public Func<IEntityProperties, bool> PrimaryLights =>
        e => IsEntityMatch(e, EntityType.Light) && (string?)e.Attribute!.type != "secondary";

    public Func<IEntityProperties, bool> SecondaryLights =>
        e => IsEntityMatch(e, EntityType.Light) && (string?)e.Attribute!.type == "secondary";

    public Func<IEntityProperties, bool> Lights => e => PrimaryLights(e) || SecondaryLights(e);

    public Func<IEntityProperties, bool> Workstations => e => IsEntityMatch(e, EntityType.WorkStation);
    public Func<IEntityProperties, bool> EntryPoints =>
        e => IsEntityMatch(e, EntityType.BinarySensor, DeviceClass.Door, DeviceClass.Window) ||
             IsEntityMatch(e, EntityType.Cover, DeviceClass.Garage);
    protected virtual Func<IEntityProperties, bool> AllOccupancySensors => e => MotionSensors(e) || OccupancySensors(e);

    protected virtual TimeSpan PowerSensorOffDebounce => TimeSpan.FromMinutes(5);
    protected virtual TimeSpan PowerSensorOnDebounce => TimeSpan.FromSeconds(30);
    protected virtual TimeSpan MediaPlayerDebounce => TimeSpan.FromMinutes(1);
    protected virtual TimeSpan WorkstationOffDebounce => TimeSpan.FromMinutes(1);

    private string RoomPresenceEntityName => $"input_boolean.presence_{RoomName.ToLower()}";
    private string TimerEntityName => $"timer.occupancy_{RoomName.ToLower()}";

    protected Func<IEntityProperties, bool> MasterOffSwitches => e => IsEntityMatch(e, EntityType.Sensor) && e.Attribute!.switch_type == SwitchType.MasterOff.AsString(EnumFormat.DisplayName, EnumFormat.Name)!.ToLower();
    public override void Initialize()
    {
        if (!string.IsNullOrWhiteSpace(SingleRoomModeName) && RoomName != SingleRoomModeName)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Single room mode, ignoring room: " + RoomName);
            Console.ForegroundColor = ConsoleColor.White;
            return;
        }

        if (AutoDiscoverDevices)
        {
            var roomPresence = Entity(RoomPresenceEntityName);
            var roomPresenceState = State(RoomPresenceEntityName);
            if (roomPresenceState == null)
                throw new Exception("Could not find room presence input boolean");

            LogDiscoveredEntities();
            DebugLog("Occupancy Timeout: {timeout} minute(s)", OccupancyTimeoutObserved.TotalMinutes);
            
            var timer = Entity(TimerEntityName);
            var timerState = State(TimerEntityName);
            if (timerState == null)
                throw new Exception("Could not find room timer");

            var occupancySensorChanges = Entities(OccupancySensors).StateChangesFiltered();

            var motionSensorChanges = Entities(MotionSensors).StateChangesFiltered();

            var entryPointsChanges = Entities(EntryPoints).StateChangesFiltered();


            // doors/windows require timer but we exclude them from being considered for occupancy
            var entryPointsOpen = Entities(EntryPoints).StateChangesFiltered()
                .Where(s => s.Old.State == "off" && s.New.State == "on")
                .Subscribe(s => StartTimer());

            var lightsOn = Entities(Lights).StateChangesFiltered()
                .Where(s => s.Old.State == "off" && s.New.State == "on");

            var powerSensorOff =
                Entities(PowerSensors)
                    .StateChangesFiltered()
                    .Where(s =>
                        s.Old.State >= s.Old.Attribute?.active_threshold &&
                        s.New.State < s.New.Attribute?.active_threshold)
                    .NDSameStateFor(PowerSensorOffDebounce);

            var powerSensorOn =
                Entities(PowerSensors)
                    .StateChangesFiltered()
                    .Where(s =>
                        s.Old.State < s.Old.Attribute?.active_threshold &&
                        s.New.State >= s.New.Attribute?.active_threshold)
                    .NDSameStateFor(PowerSensorOnDebounce);

            var powerSensorChanges = powerSensorOff.Merge(powerSensorOn);

            var workstationOff =
                Entities(Workstations)
                    .StateChangesFiltered()
                    .Where(s => s.Old.State == "on" && s.New.State == "off")
                    .NDSameStateFor(WorkstationOffDebounce);

            var workStationOn =
                Entities(Workstations)
                    .StateChangesFiltered()
                    .Where(s => s.Old.State == "off" && s.New.State == "on");

            var mediaPlayerChanges =
                Entities(MediaPlayerDevices)
                    .StateChangesFiltered()
                    .NDSameStateFor(MediaPlayerDebounce);

            var workstationChanges = workStationOn.Merge(workstationOff);

            Observable.Merge(
                motionSensorChanges,
                entryPointsChanges,
                occupancySensorChanges,
                lightsOn,
                powerSensorChanges,
                workstationChanges,
                mediaPlayerChanges).Subscribe(tuple =>
            {
                DebugLog("State change - {entity} - {from} - {to}", tuple.Old.EntityId, tuple.Old.State,
                    tuple.New.State);

                if(AnyOccupanyMarkers() || this.AnyStatesAre(Lights, "on"))
                    StartTimer();

                
            });

            EventChanges.Where(e => (e.Event == "timer.started" || e.Event == "timer.finished") && e.Data!.entity_id == TimerEntityName)
                .Subscribe(s =>
                {
                    var occupancy = AnyOccupanyMarkers();
                    
                    if (s.Event =="timer.started" && occupancy)
                    {
                        DebugLog("Room presence set on");
                        roomPresence.TurnOn();
                    }
                    if (s.Event == "timer.finished" && !occupancy)
                    {
                        DebugLog("Room presence set off");
                        roomPresence.TurnOff();
                    }
                });

            roomPresence.StateChangesFiltered().Subscribe(s =>
            {
                if (s.New.State == "on")
                    OccupancyOn();
                else
                    OccupancyOff();
            });

            Entities(MasterOffSwitches).StateChangesFiltered()
                .Where(s => s.New.State == "single")
                .Subscribe(_ =>
                {
                    LogHistory("Turn everything off");
                    TurnEveryThingOff();
                });

            if (this.AllStatesAre(Lights, "off"))
            {
                StopTimer();
                roomPresence.TurnOff();
            }

            if (AnyOccupanyMarkers() || this.AnyStatesAre(Lights, "on"))
                StartTimer();
        }
        else
        {
            Log(LogLevel.Warning, "{Room} auto discovery is disabled", RoomName);
        }
    }

    private void LogDiscoveredEntities()
    {
        if (!DebugMode) return;

        DebugLog("==============================================");
        DebugLog("Room discovery");
        DebugEntityDiscovery(MotionSensors, nameof(MotionSensors));
        DebugEntityDiscovery(PowerSensors, nameof(PowerSensors));
        DebugEntityDiscovery(MediaPlayerDevices, nameof(MediaPlayerDevices));
        DebugEntityDiscovery(PrimaryLights, nameof(PrimaryLights));
        DebugEntityDiscovery(SecondaryLights, nameof(SecondaryLights));
        DebugEntityDiscovery(EntryPoints, nameof(EntryPoints));
        DebugEntityDiscovery(Workstations, nameof(Workstations));
        DebugEntityDiscovery(OccupancySensors, nameof(OccupancySensors));
        DebugEntityDiscovery(MasterOffSwitches, nameof(MasterOffSwitches));
        DebugLog("==============================================");
    }

    private void DebugEntityDiscovery(Func<IEntityProperties, bool> searcher, string description)
    {
        var humanDescription = description.Humanize(LetterCasing.LowerCase);
        DebugLog("Searching for {description}", humanDescription);

        var states = States.Where(searcher).ToList();

        DebugLog("{count} {description} found", states.Count, humanDescription);

        foreach (var entity in states)
        {
            DebugLog("Found {description}: {entity}", humanDescription, entity.EntityId);
        }
    }

    public bool AnyOccupanyMarkers()
    {
        DebugLog("Checking occupancy markers");
        DebugLog("-----------------------");

        foreach (var os in States.Where(e => AllOccupancySensors(e) || MediaPlayerDevices(e) || Workstations(e) || Lights(e)))
        {
            DebugLog("{os}:{state}", os.EntityId, os.State);
        }

        foreach (var ps in States.Where(e => PowerSensors(e)))
        {
            DebugLog("{ps}:{state}W - Threshold {threshold}W - Above threshold: {threshholdmet}", ps.EntityId, ps.State,
                ps.Attribute!.active_threshold, ps.State >= ps.Attribute.active_threshold);
        }

        DebugLog("Occupancy timer is running: {timer}",State(TimerEntityName)?.State);
        DebugLog("-----------------------");

        var occupancy = this.AnyStatesAre(AllOccupancySensors, "on", "open");
        //var entry = this.AnyStatesAre(EntryPoints, "on");
        var media = this.AnyStatesAre(MediaPlayerDevices, "playing");
        var workstation = this.AnyStatesAre(Workstations, "home");
        var power = this.AnyStatesAre(PowerSensors, p => p.State >= p.Attribute!.active_threshold);
        var timer = State(TimerEntityName)!.State != "idle";

        return occupancy || 
               //entry || 
               media || 
               workstation || 
               power || 
               timer;
    }

    public void OccupancyOn()
    {
        ToggleLights(true);
    }

    public void OccupancyOff()
    {
        ToggleLights(false);
    }

    protected virtual void TurnEveryThingOff()
    {
        this.TurnEverythingOff();
    }

    public void StartTimer()
    {
        DebugLog("Starting timer");

        CallService("timer", "cancel", new {entity_id = TimerEntityName});
        CallService("timer", "start", new {entity_id = TimerEntityName, duration = OccupancyTimeoutObserved.ToString()});
    }

    public void StopTimer()
    {
        DebugLog("Stopping timer");

        CallService("timer", "finish", new { entity_id = TimerEntityName });
    }


    private bool IsEntityMatch(IEntityProperties prop, EntityType entityType, params DeviceClass[] deviceClasses)
    {
        var entityString = entityType.AsString(EnumFormat.DisplayName, EnumFormat.Name)!.ToLower();
        var deviceStrings = deviceClasses.Select(t => t.AsString(EnumFormat.DisplayName, EnumFormat.Name)!.ToLower()).ToList();

        var areas = prop.Attribute?.area;

        if (areas == null)
            return false;

        if (!((string)areas).Split(",").Contains(RoomName.ToLower()))
            return false;

        if (prop.EntityId.ToLower().Split(".")[0] != entityString)
            return false;

        if (deviceStrings == null || !deviceStrings.Any())
            return true;

        return deviceStrings.Contains(prop.Attribute?.device_class);
    }

    public void ToggleLights(bool on)
    {
        
        DebugLog("Toggle lights: {on}", on);

        var primaryLights = Entities(PrimaryLights);
        var secondaryLights = Entities(SecondaryLights);
        if (@on)
        {
            if (!this.AllStatesAre(PrimaryLights, "on") && AutomatedLightsOn)
            {
                LogHistory($"Turning lights on");
                primaryLights.TurnOn();
            }

            if (!this.AllStatesAre(SecondaryLights, "on") && SecondaryLightingEnabled)
            {
                LogHistory($"Turning secondary lights on");

                if (SecondaryLightingAttributes != null)
                {
                    secondaryLights.TurnOn(SecondaryLightingAttributes);
                }
                else
                {
                    secondaryLights.TurnOn();
                }
            }
        }
        else if (!on)
        {
            if (!this.AllStatesAre(PrimaryLights, "off") && AutomatedLightsOff)
            {
                LogHistory($"Turning lights off");
                primaryLights.TurnOff();
                secondaryLights.TurnOff();
            }
        }
    }

    protected void DebugLog(string message, params object[] data)
    {
        if (DebugMode)
            Log(LogLevel.Information, message, data);
    }

    public void LogHistory(string automation)
    {
        LogHelper.Log(this,RoomName.Humanize(),automation);
    }
}

public static class EntityStateExtensions
{
    public static IObservable<(EntityState Old, EntityState New)> StateChangesFiltered(this RxEntity entity)
    {
        return entity.StateChanges.Where(s => s.New.State != null && s.Old.State != s.New.State);
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