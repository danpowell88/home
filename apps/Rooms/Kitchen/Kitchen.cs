using System;
using JetBrains.Annotations;
using NetDaemon.Common.Fluent;

[UsedImplicitly]
public class Kitchen : RoomApp
{
    protected override bool IndoorRoom => true;
    protected override TimeSpan OccupancyTimeout => TimeSpan.FromMinutes(10);

    private readonly Func<IEntityProperties, bool> _dishwasherStatus = e => e.EntityId == "input_select.dishwasher_status";
    private readonly Func<IEntityProperties, bool> _dishwasherPowerSensor = e => e.EntityId == "switch.dishwasher";
    private readonly Func<IEntityProperties, bool> _dishwasherDoor = e => e.EntityId == "binary_sensor.dishwasher_door_contact";

    protected override bool SecondaryLightingEnabled => DateTime.Now.Hour >= 18 && DateTime.Now.Hour <= 22;

    public override void Initialize()
    {
        //SetupDishwasher();

        //Entity("binary_sensor.fridge_door_contact")
        //    .WhenStateChange((to, from) => from!.State == "off" && to.State == "on")
        //    .Call(async (_, __, ___) =>
        //    {
        //        if ((GetState("person.daniel")!.State != "home" ||
        //             GetState("binary_sensor.media_chair_right_occupancy")!.State == "on") &&
        //            DateTime.Now.Hour > 15 && DateTime.Now.Hour < 20 &&
        //            ((DateTime?)Storage.LastFridgeNotification == null ||
        //            DateTime.Now - (DateTime) Storage.LastFridgeNotification >=
        //            TimeSpan.FromHours(24)))
        //        {
        //            await this.Notify(new Uri("http://192.168.1.2:8123/local/big_pig_snort.mp3"), 0.4M,Notifier.AudioNotificationDevice.Kitchen);
        //            Storage.LastFridgeNotification = DateTime.Now;
        //        }
        //    })
        //    .Execute();

        base.Initialize();
    }

   // private void SetupDishwasher()
  //  {
        //Entities(_dishwasherPowerSensor)
        //    .WhenStateChange((to, from) =>
        //    {
        //        var resetStates = new List<DishwasherState> {DishwasherState.Dirty, DishwasherState.Clean};

        //        return GetDishwasherWattage(to!) > 10D &&
        //               resetStates.Contains(GetDishwasherState());
        //    })
        //    .Call(async (_, __, ___) =>
        //        await InputSelects(_dishwasherStatus).SetOption(DishwasherState.Running).ExecuteAsync())
        //    .Execute();

        //Entities(_dishwasherPowerSensor)
        //    .WhenStateChange((to, from) =>
        //        GetDishwasherWattage(to!) < 1D &&
        //        GetDishwasherState() == DishwasherState.Running)
        //    .AndNotChangeFor(new TimeSpan(0, 1, 45))
        //    .Call(async (_, __, ___) =>
        //        await InputSelects(_dishwasherStatus).SetOption(DishwasherState.Clean).ExecuteAsync())
        //    .Execute();

        //Entities(_dishwasherDoor)
        //    .WhenStateChange((to, from) =>
        //        @from!.State == "off" &&
        //        to!.State == "on" &&
        //        (
        //            GetDishwasherWattage(to!) < 1D ||
        //            GetDishwasherState() == DishwasherState.Clean)
        //    )
        //    .Call(async (_, __, ___) =>
        //        await InputSelects(_dishwasherStatus).SetOption(DishwasherState.Dirty).ExecuteAsync())
        //    .Execute();

        //Entities(_dishwasherStatus)
        //    .WhenStateChange((to, from) =>
        //        @from!.State == DishwasherState.Running.ToString("F") &&
        //        to!.State == DishwasherState.Clean.ToString("F"))
        //    .Call(async (_, __, ___) =>
        //        await this.Notify(
        //            "Kitchen",
        //            "The dishwasher has finished",
        //            Notifier.NotificationCriteria.NotSleeping,
        //            Notifier.NotificationCriteria.NotSleeping,
        //            Notifier.TextNotificationDevice.All))
        //    .Execute();
   // }

    //private static double GetDishwasherWattage(EntityState to)
    //{
    //    return to!.Attribute!.current_power_w ?? 0D;
    //}

    //private DishwasherState GetDishwasherState()
    //{
    //    return Enums.Parse<DishwasherState>(State.Single(_dishwasherStatus).State);
    //}

    private enum DishwasherState
    {
        Dirty,
        Running,
        Clean
    }
}