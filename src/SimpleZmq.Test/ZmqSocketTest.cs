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
    }
}
