using System;
using System.Linq;
using System.Threading.Tasks;
using JoySoftware.HomeAssistant.NetDaemon.Common;

public class Notification : NetDaemonApp
{
    public override Task InitializeAsync()
    {
        // bed occupancy count < people home count && bed occupancy > 0
        // 

        //Scheduler.RunEvery(new TimeSpan(0, 30, 0), async () =>
        //{
        //    await CallService("media_player", "set_volume", new
        //    {
        //        entity_id = "home"`,
        //        volume_level = 0.5
        //    });
        //});

        


        return base.InitializeAsync();
    }
}