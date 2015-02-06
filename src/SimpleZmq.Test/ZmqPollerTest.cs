using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SimpleZmq.Test
{
    [TestClass]
    public class ZmqPollerTest
    {
        [TestMethod]
        public void Polling_From_Two_Sockets()
        {
            using (var zmqServerContext = new ZmqContext())
            using (var zmqClientContext = new ZmqContext())
            {
                using (ZmqSocket pushSocket1 = zmqServerContext.CreateSocket(ZmqSocketType.Push))
                using (ZmqSocket pushSocket2 = zmqServerContext.CreateSocket(ZmqSocketType.Push))
                {
                    pushSocket1.Bind("tcp://127.0.0.1:5555");
                    pushSocket2.Bind("tcp://127.0.0.1:5556");

                    var pullSocketsReady = new ManualResetEventSlim();

                    var clientPolling = Task.Run(() =>
                    {
                        using (ZmqSocket pullSocket1 = zmqClientContext.CreateSocket(ZmqSocketType.Pull))
                        using (ZmqSocket pullSocket2 = zmqClientContext.CreateSocket(ZmqSocketType.Pull))
                        {
                            pullSocket1.Connect("tcp://127.0.0.1:5555");
                            pullSocket2.Connect("tcp://127.0.0.1:5556");

                            int numberOfExpectedMessagesIntoSocket1 = 1;
                            int numberOfExpectedMessagesIntoSocket2 = 1;

                            var poller = ZmqPoller.New()
                                .HandleEventsOf(
                                    new[] { pullSocket1, pullSocket2 },
                                    s =>
                                    {
                                        Assert.IsFalse(s.HasMoreToReceive);     // after polling, the socket doesn't seem to have more to receive, although it has.
                                        var buffer = new byte[4];
                                        int receivedLength;
                                        var receivedBuffer = s.Receive(buffer, out receivedLength, doNotWait: true);
                                        Assert.IsNotNull(receivedBuffer);
                                        Assert.AreSame(buffer, receivedBuffer);
                                        if (ReferenceEquals(s, pullSocket1))
                                        {
                                            Assert.AreEqual(receivedLength, 4);
                                            CollectionAssert.AreEqual(receivedBuffer, new byte[] { 1, 2, 3, 4 });
                                            numberOfExpectedMessagesIntoSocket1--;
                                        }
                                        else if (ReferenceEquals(s, pullSocket2))
                                        {
                                            Assert.AreEqual(receivedLength, 4);
                                            CollectionAssert.AreEqual(receivedBuffer, new byte[] { 11, 12, 13, 14 });
                                            numberOfExpectedMessagesIntoSocket2--;
                                        }
                                        else
                                        {
                                            Assert.Fail();
                                        }
                                    }
                                 )
                                .Build();
                            pullSocketsReady.Set();
                            while (numberOfExpectedMessagesIntoSocket1 > 0 || numberOfExpectedMessagesIntoSocket2 > 0)
                            {
                                poller.Poll(1000);
                            }
                        }
                    });

                    pullSocketsReady.Wait();

                    Assert.IsTrue(pushSocket1.Send(new byte[] { 1, 2, 3, 4 }, 4));
                    Assert.IsTrue(pushSocket2.Send(new byte[] { 11, 12, 13, 14 }, 4));
                    clientPolling.Wait();
                }
            }
        }
    }
}
