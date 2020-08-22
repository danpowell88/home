using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using EnumsNET;
using NetDaemon.Common.Fluent;

namespace daemonapp.Utilities
{
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
    public static class EntityLocator
    {
        public static Func<IEntityProperties, bool> MotionSensors(string roomName) => e => IsEntityMatch(roomName,e, EntityType.BinarySensor, DeviceClass.Motion);
        public static Func<IEntityProperties, bool> OccupancySensors(string roomName) => e => IsEntityMatch(roomName, e, EntityType.BinarySensor, DeviceClass.Occupancy);
        public static Func<IEntityProperties, bool> PowerSensors(string roomName) => e => IsEntityMatch(roomName, e, EntityType.Sensor, DeviceClass.Power) && e.Attribute!.active_threshold != null;
        public static Func<IEntityProperties, bool> MediaPlayerDevices(string roomName) => e => IsEntityMatch(roomName, e, EntityType.MediaPlayer);
        public static Func<IEntityProperties, bool> PrimaryLights(string roomName) =>
            e => IsEntityMatch(roomName, e, EntityType.Light) && (string?)e.Attribute!.type != "secondary";

        public static Func<IEntityProperties, bool> SecondaryLights(string roomName) =>
            e => IsEntityMatch(roomName, e, EntityType.Light) && (string?)e.Attribute!.type == "secondary";

        public static Func<IEntityProperties, bool> Lights(string roomName) =>
            e => PrimaryLights(roomName)(e) || SecondaryLights(roomName)(e);

        public static Func<IEntityProperties, bool> Workstations(string roomName) => e => IsEntityMatch(roomName, e, EntityType.WorkStation);
        public static Func<IEntityProperties, bool> EntryPoints(string roomName) =>
            e => IsEntityMatch(roomName, e, EntityType.BinarySensor, DeviceClass.Door, DeviceClass.Window) ||
                             IsEntityMatch(roomName, e, EntityType.Cover, DeviceClass.Garage);

        public static Func<IEntityProperties, bool> MasterOffSwitches(string roomName) => e => IsEntityMatch(roomName,e, EntityType.Sensor) && e.Attribute!.switch_type == SwitchType.MasterOff.AsString(EnumFormat.DisplayName, EnumFormat.Name)!.ToLower();

        public static string RoomPresenceEntityName(string roomName) => $"input_boolean.presence_{roomName.ToLower()}";
        public static string TimerEntityName(string roomName) => $"timer.occupancy_{roomName.ToLower()}";

        private static bool IsEntityMatch(string roomName, IEntityProperties prop, EntityType entityType, params DeviceClass[] deviceClasses)
        {
            var entityString = entityType.AsString(EnumFormat.DisplayName, EnumFormat.Name)!.ToLower();
            var deviceStrings = deviceClasses.Select(t => t.AsString(EnumFormat.DisplayName, EnumFormat.Name)!.ToLower()).ToList();

            var areas = prop.Attribute?.area;

            if (areas == null)
                return false;

            if (!((string)areas).Split(",").Contains(roomName.ToLower()))
                return false;

            if (prop.EntityId.ToLower().Split(".")[0] != entityString)
                return false;

            if (deviceStrings == null || !deviceStrings.Any())
                return true;

            return deviceStrings.Contains(prop.Attribute?.device_class);
        }
    }
}
