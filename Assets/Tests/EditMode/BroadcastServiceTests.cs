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

        [Test]
        public void ThrowingSubscriber_DoesNotBlockOthers()
        {
            // Regression: previously a throwing handler short-circuited the
            // multicast chain (later subscribers never ran) AND propagated
            // up to the Send caller — which inside GameClockService.Tick
            // froze the game loop. Send now traps per-subscriber.
            int before = 0, after = 0;
            BroadcastService.Subscribe<PingMsg>(_ => before++);
            BroadcastService.Subscribe<PingMsg>(_ => throw new System.InvalidOperationException("boom"));
            BroadcastService.Subscribe<PingMsg>(_ => after++);

            // Expect the throw to be swallowed inside Send → broadcast log warning.
            UnityEngine.TestTools.LogAssert.Expect(UnityEngine.LogType.Warning, new System.Text.RegularExpressions.Regex("subscriber #1 threw"));

            Assert.DoesNotThrow(() => BroadcastService.Send(new PingMsg(7)));
            Assert.AreEqual(1, before, "earlier subscriber should still fire");
            Assert.AreEqual(1, after,  "later subscriber should still fire despite middle throw");
        }
    }
}
