using Microsoft.VisualStudio.TestTools.UnitTesting;
using MSTestExtensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SimpleZmq.Test
{
    [TestClass]
    public class ZmqSocketTest
    {
        [TestMethod]
        public void Socket_Cannot_Be_Bound_With_Null_EndPoint()
        {
            using (var zmqContext = new ZmqContext())
            {
                using (ZmqSocket socket = zmqContext.CreateSocket(SocketType.Push))
                {
                    ExceptionAssert.Throws<ArgumentNullException>(() => socket.Bind(null));
                }
            }
        }

        [TestMethod]
        public void Socket_Cannot_Be_Bound_With_Empty_EndPoint()
        {
            using (var zmqContext = new ZmqContext())
            {
                using (ZmqSocket socket = zmqContext.CreateSocket(SocketType.Push))
                {
                    ExceptionAssert.Throws<ArgumentException>(() => socket.Bind(""));
                }
            }
        }

        [TestMethod]
        public void Socket_Cannot_Be_Bound_With_WhiteSpace_EndPoint()
        {
            using (var zmqContext = new ZmqContext())
            {
                using (ZmqSocket socket = zmqContext.CreateSocket(SocketType.Push))
                {
                    ExceptionAssert.Throws<ArgumentException>(() => socket.Bind("    "));
                }
            }
        }

        [TestMethod]
        public void Socket_Cannot_Be_Bound_With_Invalid_Protocol()
        {
            using (var zmqContext = new ZmqContext())
            {
                using (ZmqSocket socket = zmqContext.CreateSocket(SocketType.Push))
                {
                    ExceptionAssert.Throws<ZmqException>(() => socket.Bind("invalid://test"), "Protocol not supported (135)");
                }
            }
        }

        [TestMethod]
        public void Get_And_Set_Socket_Options()
        {
            using (var zmqContext = new ZmqContext())
            {
                using (ZmqSocket socket = zmqContext.CreateSocket(SocketType.Push))
                {
                    Assert.AreEqual(socket.SendHWM, 1000);
                    socket.SendHWM = 0;
                    Assert.AreEqual(socket.SendHWM, 0);

                    socket.SendHWM = 10;
                    Assert.AreEqual(socket.SendHWM, 10);

                    Assert.AreEqual(socket.Linger, -1);
                    socket.Linger = 0;
                    Assert.AreEqual(socket.Linger, 0);

                    socket.Linger = 500;
                    Assert.AreEqual(socket.Linger, 500);
                }
            }
        }

        [TestMethod]
        public void Socket_Connect_To_Bound_Socket_And_Send()
        {
            using (var zmqContext = new ZmqContext())
            {
                using (ZmqSocket pushSocket = zmqContext.CreateSocket(SocketType.Push))
                using (ZmqSocket pullSocket = zmqContext.CreateSocket(SocketType.Pull))
                {
                    pushSocket.Bind("tcp://127.0.0.1:5555");
                    pullSocket.Connect("tcp://127.0.0.1:5555");

                    pushSocket.Send(new byte[] { 1,2,3,4 }, 4);
                }
            }
        }
    }
}
