using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SimpleZmq.Test
{
    [TestClass]
    public class ZmqContextTest
    {
        [TestMethod]
        public void Create_Configure_And_Destroy_Context()
        {
            using (var zmqContext = new ZmqContext())
            {
                Assert.AreEqual(zmqContext.NumberOfIoThreads, ZmqContext.DefaultNumberOfIoThreads);
                Assert.AreEqual(zmqContext.MaxNumberOfSockets, ZmqContext.DefaultMaxNumberOfSockets);
                Assert.IsFalse(zmqContext.IPv6);

                zmqContext.NumberOfIoThreads = 4;
                Assert.AreEqual(zmqContext.NumberOfIoThreads, 4);

                zmqContext.MaxNumberOfSockets = 512;
                Assert.AreEqual(zmqContext.MaxNumberOfSockets, 512);

                zmqContext.IPv6 = true;
                Assert.AreEqual(zmqContext.IPv6, true);
            }
        }

        [TestMethod]
        public void Create_And_Destroy_Sockets()
        {
            using (var zmqContext = new ZmqContext())
            {
                using (ZmqSocket pushSocket = zmqContext.CreateSocket(SocketType.Push), pullSocket = zmqContext.CreateSocket(SocketType.Pull))
                {
                    pushSocket.Bind("inproc://test");
                    pullSocket.Connect("inproc://test");
                }
            }
        }
    }
}
