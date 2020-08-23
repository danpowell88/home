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
    protected virtual string RoomName => GetType().Name;

    private readonly string? SingleRoomModeName = "";

    // ReSharper disable once RedundantLogicalConditionalExpressionOperand
    protected virtual bool DebugMode => SingleRoomModeName == RoomName || false;

    protected abstract bool IndoorRoom { get; }

    protected virtual bool AutomatedLightsOn => State(EntityLocator.MotionEntityName(IndoorRoom))?.State == "on";
    protected virtual bool AutomatedLightsOff => true;

    protected virtual bool AutoDiscoverDevices => true;

    protected virtual bool SecondaryLightingEnabled => false;
    protected virtual Dictionary<string, object>? SecondaryLightingAttributes => null;

    protected TimeSpan OccupancyTimeoutObserved => DebugMode ? OccupancyTimeoutTest : OccupancyTimeout;
    private TimeSpan OccupancyTimeoutTest => TimeSpan.FromMinutes(1);
    protected virtual TimeSpan OccupancyTimeout => TimeSpan.FromMinutes(3);

    protected virtual TimeSpan PowerSensorDebounce => TimeSpan.FromSeconds(30);
    protected virtual TimeSpan MediaPlayerDebounce => TimeSpan.FromMinutes(1);
    protected virtual TimeSpan WorkstationDebounce => TimeSpan.FromMinutes(1);

    bool OffToOn((EntityState Old, EntityState New) s)
    {
        return s.Old.State == "off" && s.New.State == "on";
    }

    bool OnToOff((EntityState Old, EntityState New) s)
    {
        return s.Old.State == "on" && s.New.State == "off";
    }

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
            var roomPresence = GetRoomPresenceAndValidateRequiredEntities();

            LogDiscoveredEntities();
            DebugLog("Occupancy Timeout: {timeout} minute(s)", OccupancyTimeoutObserved.TotalMinutes);

            // lights and doors dont indicate occupancy themselves but should start a timeout
            WireUpNonOccupancyMarkers();
            WireUpOccupancyMarkers(roomPresence);
            SetupGenericAutomations();
            ReInitaliseRoomState(roomPresence);
        }
        else
        {
            Log(LogLevel.Warning, "{Room} auto discovery is disabled", RoomName);
        }
    }

    private void WireUpOccupancyMarkers(RxEntity roomPresence)
    {
        var occupancySensors = Entities(EntityLocator.OccupancySensors(RoomName)).StateChangesFiltered();
        var powerSensors = GetPowerSensors();
        var workstations = GetWorkstations();
        var mediaPlayers = GetMediaPlayers();

        var timerChanges = GetTimerChanges();

        Observable
            .Merge(
                occupancySensors,
                powerSensors,
                workstations,
                mediaPlayers,
                timerChanges)
            .Synchronize()
            .Subscribe(s =>
            {
                DebugLog("State change - {entity} - {from} - {to}", s.Old.EntityId, s.Old.State, s.New.State);

                if (AnyOccupanyMarkers())
                {
                    DebugLog("Room presence set on");
                    roomPresence.TurnOn();
                }
                else
                {
                    DebugLog("Room presence set off");
                    roomPresence.TurnOff();
                }
            });

        roomPresence.StateChanges.Synchronize().Subscribe(s =>
        {
            if (s.New.State == "on")
                OccupancyOn();
            else
                OccupancyOff();
        });

        // if motion is enabled then turn on lights that are marked for enabled
        // this shoudl solve if a sensor would have turned on lights but motion was disabled when it went off
        Entity(EntityLocator.MotionEntityName(IndoorRoom)).StateChangesFiltered().Where(OffToOn).Subscribe(_ =>
        {
            StartTimer();
            ToggleLights(true);
        });
    }

    private void WireUpNonOccupancyMarkers()
    {
        // When motion is detected or light is turned on start timer and toggle lights on (adhering to automated lighting rules) 
        Observable.Merge(
                Entities(EntityLocator.MotionSensors(RoomName)).StateChangesFiltered().Where(OffToOn),
                Entities(EntityLocator.Lights(RoomName)).StateChangesFiltered().Where(OffToOn),
                Entities(EntityLocator.EntryPoints(RoomName)).StateChangesFiltered()) // dont know if you are entering or leaving (so we'll just trigger the lights and timer)
            .Synchronize()
            .Subscribe(s =>
            {
                StartTimer();
                ToggleLights(true);
            });

        // handles the case where someone manually switches the lights off (not by an automation, ie. by hand or HA GUI)
        // also when door is closed 
        Entities(EntityLocator.Lights(RoomName)).StateChangesFiltered().Where(OnToOff)
            .Synchronize()
            .Subscribe(s => { StopTimer(); });
    }

    private IObservable<(EntityState Old, EntityState New)> GetTimerChanges()
    {
        return EventChanges
            .Synchronize()
            .Where(e => (e.Event == "timer.finished" || e.Event == "timer.started") && e.Data!.entity_id == EntityLocator.TimerEntityName(RoomName))
            .Select<RxEvent, (EntityState Old, EntityState New)>(e => (
                new EntityState {EntityId = e.Data!.entity_id, State = e.Event == "timer.finished" ? "on" : "off"},
                new EntityState { EntityId = e.Data!.entity_id, State = e.Event == "timer.finished" ? "off" : "on" }));
    }

    private void SetupGenericAutomations()
    {
        Entities(EntityLocator.MasterOffSwitches(RoomName)).StateChangesFiltered()
            .Synchronize()
            .Where(s => s.New.State == "single")
            .Subscribe(_ =>
            {
                LogHistory("Turn everything off");
                TurnEveryThingOff();
            });
    }

    private void ReInitaliseRoomState(RxEntity roomPresence)
    {
        if (this.AllStatesAre(EntityLocator.Lights(RoomName), "off"))
        {
            StopTimer();
            roomPresence.TurnOff();
        }

        if (AnyOccupanyMarkers() || this.AnyStatesAre(EntityLocator.Lights(RoomName), "on"))
            StartTimer();
    }

    private RxEntity GetRoomPresenceAndValidateRequiredEntities()
    {
        var roomPresence = Entity(EntityLocator.RoomPresenceEntityName(RoomName));
        var roomPresenceState = State(EntityLocator.RoomPresenceEntityName(RoomName));
        if (roomPresenceState == null)
            throw new Exception("Could not find room presence input boolean");

        var timerState = State(EntityLocator.TimerEntityName(RoomName));
        if (timerState == null)
            throw new Exception("Could not find room timer");
        return roomPresence;
    }

    private IObservable<(EntityState Old, EntityState New)> GetMediaPlayers()
    {
        var mediaPlayerEntityIds = States.Where(EntityLocator.MediaPlayerDevices(RoomName)).Select(s => s.EntityId);
        var mediaPlayerSensorsList = new List<IObservable<(EntityState Old, EntityState New)>>();

        foreach (var entityId in mediaPlayerEntityIds)
        {
            mediaPlayerSensorsList.Add(
                Entity(entityId)
                    .StateChangesFiltered()
                    .Select<(EntityState Old, EntityState New),
                        (EntityState Old, EntityState New)>
                    (e => (
                        new EntityState
                        {
                            EntityId = e.Old.EntityId,
                            State = e.Old.State == "playing" ? "on" : "off"
                        },
                        new EntityState
                        {
                            EntityId = e.New.EntityId,
                            State = e.New.State == "playing" ? "on" : "off"
                        }))
                    .NDSameStateFor(MediaPlayerDebounce));
        }

        var mediaPlayers = mediaPlayerSensorsList.Merge();
        return mediaPlayers;
    }

    private IObservable<(EntityState Old, EntityState New)> GetWorkstations()
    {
        var workstationEntityIds = States.Where(EntityLocator.Workstations(RoomName)).Select(s => s.EntityId);
        var workstationSensorsList = new List<IObservable<(EntityState Old, EntityState New)>>();

        foreach (var entityId in workstationEntityIds)
        {
            workstationSensorsList.Add(
                Entity(entityId)
                    .StateChangesFiltered()
                    .Select<(EntityState Old, EntityState New),
                        (EntityState Old, EntityState New)>
                    (e => (
                        new EntityState
                        {
                            EntityId = e.Old.EntityId,
                            State = e.Old.State
                        },
                        new EntityState
                        {
                            EntityId = e.New.EntityId,
                            State = e.New.State
                        }))
                    .NDSameStateFor(WorkstationDebounce));
        }

        var workstations = workstationSensorsList.Merge();
        return workstations;
    }

    private IObservable<(EntityState Old, EntityState New)> GetPowerSensors()
    {
        var powerSensorEntityIds = States.Where(EntityLocator.PowerSensors(RoomName)).Select(s => s.EntityId);
        var powerSensorsList = new List<IObservable<(EntityState Old, EntityState New)>>();

        foreach (var entityId in powerSensorEntityIds)
        {
            powerSensorsList.Add(
                Entity(entityId)
                    .StateChangesFiltered()
                    .Select<(EntityState Old, EntityState New),
                        (EntityState Old, EntityState New)>
                    (e => (
                        new EntityState
                        {
                            EntityId = e.Old.EntityId,
                            State = e.Old.State >= e.Old.Attribute!.active_threshold ? "on" : "off"
                        },
                        new EntityState
                        {
                            EntityId = e.New.EntityId,
                            State = e.New.State >= e.New.Attribute!.active_threshold ? "on" : "off"
                        }))
                    .NDSameStateFor(PowerSensorDebounce));
        }

        var powerSensors = powerSensorsList.Merge();
        return powerSensors;
    }

    private void LogDiscoveredEntities()
    {
        if (!DebugMode) return;

        DebugLog("==============================================");
        DebugLog("Room discovery");
        DebugEntityDiscovery(EntityLocator.MotionSensors(RoomName), "MotionSensors");
        DebugEntityDiscovery(EntityLocator.PowerSensors(RoomName), "PowerSensors");
        DebugEntityDiscovery(EntityLocator.MediaPlayerDevices(RoomName), "MediaPlayerDevices");
        DebugEntityDiscovery(EntityLocator.PrimaryLights(RoomName), "PrimaryLights");
        DebugEntityDiscovery(EntityLocator.SecondaryLights(RoomName), "SecondaryLights");
        DebugEntityDiscovery(EntityLocator.EntryPoints(RoomName), "EntryPoints");
        DebugEntityDiscovery(EntityLocator.Workstations(RoomName), "Workstations");
        DebugEntityDiscovery(EntityLocator.OccupancySensors(RoomName), "OccupancySensors");
        DebugEntityDiscovery(EntityLocator.MasterOffSwitches(RoomName), "MasterOffSwitches");
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

        foreach (var os in States.Where(e =>
            EntityLocator.OccupancySensors(RoomName)(e) || EntityLocator.MediaPlayerDevices(RoomName)(e) ||
            EntityLocator.Workstations(RoomName)(e) || EntityLocator.Lights(RoomName)(e)))
        {
            DebugLog("{os}:{state}", os.EntityId, os.State);
        }

        foreach (var ps in States.Where(e => EntityLocator.PowerSensors(RoomName)(e)))
        {
            DebugLog("{ps}:{state}W - Threshold {threshold}W - Above threshold: {threshholdmet}", ps.EntityId, ps.State,
                ps.Attribute!.active_threshold, ps.State >= ps.Attribute.active_threshold);
        }

        DebugLog("Occupancy timer state: {timer}", State(EntityLocator.TimerEntityName(RoomName))?.State);
        DebugLog("-----------------------");

        var motion = this.AnyStatesAre(EntityLocator.MotionSensors(RoomName), "on");
        var occupancy = this.AnyStatesAre(EntityLocator.OccupancySensors(RoomName), "on", "open");
        var media = this.AnyStatesAre(EntityLocator.MediaPlayerDevices(RoomName), "playing");
        var workstation = this.AnyStatesAre(EntityLocator.Workstations(RoomName), "home");
        var power = this.AnyStatesAre(EntityLocator.PowerSensors(RoomName),
            p => p.State >= p.Attribute!.active_threshold);
        var timer = State(EntityLocator.TimerEntityName(RoomName))!.State == "active";

        return
            motion || // include motion as motion may never actually turn off if its consistently triggered on, and hence the timer will not be running
            occupancy ||
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

        CallService("timer", "cancel", new { entity_id = EntityLocator.TimerEntityName(RoomName) });
        CallService("timer", "start", new { entity_id = EntityLocator.TimerEntityName(RoomName), duration = OccupancyTimeoutObserved.ToString() });
    }

    public void StopTimer()
    {
        DebugLog("Stopping timer");

        CallService("timer", "finish", new { entity_id = EntityLocator.TimerEntityName(RoomName) });
    }

    public void ToggleLights(bool on)
    {

        DebugLog("Toggle lights: {on}", on);

        var primaryLights = Entities(EntityLocator.PrimaryLights(RoomName));
        var secondaryLights = Entities(EntityLocator.SecondaryLights(RoomName));
        if (@on)
        {
            if (!this.AllStatesAre(EntityLocator.PrimaryLights(RoomName), "on") && AutomatedLightsOn)
            {
                LogHistory($"Turning lights on");
                primaryLights.TurnOn();
            }

            if (!this.AllStatesAre(EntityLocator.SecondaryLights(RoomName), "on") && SecondaryLightingEnabled)
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
            if (!this.AllStatesAre(EntityLocator.PrimaryLights(RoomName), "off") && AutomatedLightsOff)
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
        LogHelper.Log(this, RoomName.Humanize(), automation);
    }
}