using NUnit.Framework;
using TrainAI.Core;

namespace TrainAI.EditMode.Tests
{
    public class BroadcastServiceTests
    {
        public readonly struct PingMsg { public readonly int value; public PingMsg(int v) { value = v; } }

        [SetUp] public void Setup() => BroadcastService.Clear();
        [TearDown] public void Teardown() => BroadcastService.Clear();

        [Test]
        public void Subscribe_Send_DeliversToHandler()
        {
            int received = -1;
            void Handler(PingMsg m) => received = m.value;
            BroadcastService.Subscribe<PingMsg>(Handler);
            BroadcastService.Send(new PingMsg(42));
            Assert.AreEqual(42, received);
        }

        [Test]
        public void Unsubscribe_StopsDelivery()
        {
            int received = -1;
            void Handler(PingMsg m) => received = m.value;
            BroadcastService.Subscribe<PingMsg>(Handler);
            BroadcastService.Unsubscribe<PingMsg>(Handler);
            BroadcastService.Send(new PingMsg(99));
            Assert.AreEqual(-1, received);
        }

        [Test]
        public void MultipleSubscribers_AllReceive()
        {
            int a = 0, b = 0;
            BroadcastService.Subscribe<PingMsg>(m => a = m.value);
            BroadcastService.Subscribe<PingMsg>(m => b = m.value * 2);
            BroadcastService.Send(new PingMsg(5));
            Assert.AreEqual(5, a);
            Assert.AreEqual(10, b);
        }

        [Test]
        public void Clear_RemovesAllHandlers()
        {
            int hit = 0;
            BroadcastService.Subscribe<PingMsg>(_ => hit++);
            BroadcastService.Clear();
            BroadcastService.Send(new PingMsg(1));
            Assert.AreEqual(0, hit);
        }
    }
}
