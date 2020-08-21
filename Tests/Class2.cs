using System;
using System.Dynamic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using JoySoftware.HomeAssistant.Client;
using Moq;
using NetDaemon.Common;
using NetDaemon.Common.Reactive;
using Xunit;

namespace NetDaemon.Daemon.Tests.Reactive
{

    public class TestRoomApp : RoomApp
    {
        protected override string RoomName => "Test";
        protected override bool IndoorRoom => true;
    }

    public class RxAppTest : DaemonHostTestBase<TestRoomApp>
    {

        [Fact]
        public async Task CallServiceShouldCallCorrectFunction()
        {
            // ARRANGE
            var daemonTask = await GetConnectedNetDaemonTask();

            (string attribute, object value)[] x = {("entity_id", DefaultDaemonRxApp.TimerEntityName), ("duration", DefaultDaemonRxApp.OccupancyTimeoutObserved)};

            var (dynObj, expObj) = GetDynamicObject(x);

            DefaultDaemonHost.InternalState["light.test"] = new EntityState {EntityId = "light.test", State = "on", Attribute = {area = "test"}};
            DefaultDaemonHost.InternalState[DefaultDaemonRxApp.RoomPresenceEntityName] = new EntityState {EntityId = DefaultDaemonRxApp.RoomPresenceEntityName, State = "on"};
            DefaultDaemonHost.InternalState[DefaultDaemonRxApp.TimerEntityName] = new EntityState {EntityId = DefaultDaemonRxApp.TimerEntityName, State = "idle"};
            
            // ACT
            DefaultDaemonRxApp.Initialize();

            //await daemonTask; -- TODO: seems to just hang if i include this
            
            // ASSERT // TODO figure out the correct syntax here, none of them match
            // if lights are on when starting up then the timer should be called to start
            DefaultHassClientMock.VerifyCallService("timer", "start", ("entity_id", DefaultDaemonRxApp.TimerEntityName), ("duration", DefaultDaemonRxApp.OccupancyTimeoutObserved));
        }
    }
}